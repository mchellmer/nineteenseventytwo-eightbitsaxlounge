using Microsoft.AspNetCore.Authorization;

using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Endpoints;

public static class MidiEndpoints
{
    public static void AddMidiEndpoints(this WebApplication app)
    {
        app.MapPut("api/Midi/{deviceName}/reset", ResetDevice);
    }
    
    [Authorize]
    private static async Task<IResult> ResetDevice(
        ILogger logger,
        IMidiDeviceService midiDeviceService,
        IMidiDataService midiDataService,
        string deviceName)
    {
        logger.LogInformation($"Resetting device {deviceName}");
        
        // There will be one message per effect to set default active state
        // Try to update the device first then data resetting to previous state on error
        // Throw an exception if unable to update/revert data after device updated
        // Proceed if data and device in sync logging and tracking errors
        var midiDevice = await midiDataService.GetDeviceByNameAsync(deviceName);
        var deviceEffects = midiDevice.DeviceEffects;
        bool errorResettingDevice = false;
        foreach (var effect in deviceEffects)
        {
            try
            {
                logger.LogInformation(
                    $"Resetting effect default active state {effect.DefaultActive} for effect {effect.Name} on device {deviceName}");
                var activateMessage = await midiDataService.GetControlChangeMessageToActivateDeviceEffectAsync(
                    deviceName,
                    effect.Name,
                    true);
                await midiDeviceService.SendControlChangeMessageByDeviceNameAsync(deviceName, activateMessage);
                logger.LogInformation($"Effect default state reset on device");
                
                try
                {
                    logger.LogInformation($"Resetting active state data for device");
                    await midiDataService.UpdateDeviceEffectActiveStateAsync(deviceName, effect.Name, true);
                    logger.LogInformation($"Effect active state data updated");
                }
                catch (Exception e)
                {
                    logger.LogError($"Error updating active state data for device: {e.Message}");
                    try
                    {
                        logger.LogInformation($"Reverting effect active state for effect {effect.Name} on device {deviceName}");
                        errorResettingDevice = true;
                        var revertMessage = await midiDataService.GetControlChangeMessageToActivateDeviceEffectAsync(
                            deviceName,
                            effect.Name,
                            effect.Active);
                        await midiDeviceService.SendControlChangeMessageByDeviceNameAsync(deviceName, revertMessage);
                        logger.LogInformation($"Effect active state reverted on device");
                    }
                    catch (Exception exception)
                    {
                        logger.LogError(exception, "Error reverting effect active state for effect {Effect} on device {Device}: database and device out of sync",
                            effect.Name, deviceName);
                        
                        var problem = Results.Problem(
                            detail: $"Failed to revert effect '{effect.Name}' active state for device '{deviceName}', suspending reset operation: {exception.Message}",
                            title: "Device reset failed - inconsistent state",
                            statusCode: 500);

                        return problem;
                    }
                }
            } catch (Exception ex) {
                logger.LogError($"Error resetting default state on device {deviceName}: {ex.Message}");
                errorResettingDevice = true;
            }
            
            // There will be one message per effect setting of effect
            // Similarly try to update device first then data reverting on error and throw exception if unable to revert
            // DeviceEffectSetting Value is Device.(MidImplementation?MidiConfiguration.Name=DeviceEffectSetting.Name).ControlChangeValueDefault
            foreach (var setting in effect.DeviceEffectSettings)
            {
                try
                {
                    await midiDataService.GetControlChangeMessageToSetDeviceEffectSettingAsync(
                        deviceName,
                        effect.Name,
                        setting.Name,
                        setting.Value);
                }
                catch (Exception e)
                {
                    logger.LogError($"Error getting setting message for setting {setting.Name} on effect {effect.Name} on device {deviceName}: {e.Message}");
                    errorResettingDevice = true;
                }
            }
        }
        
        // Return 500 if any errors resetting device
        if (errorResettingDevice)
        {
            return Results.Problem(
                detail: $"Errors occurred resetting device '{deviceName}', please check logs for details.",
                title: "Device reset completed with errors",
                statusCode: 500);
        }
        
        // Return settings updated successfully or error
        return Results.Ok(new { Message = $"Device '{deviceName}' reset to default settings successfully." });
    }
}