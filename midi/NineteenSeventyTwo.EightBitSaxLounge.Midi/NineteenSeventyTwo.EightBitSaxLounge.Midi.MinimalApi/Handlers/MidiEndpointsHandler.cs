using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Handlers;

public class MidiEndpointsHandler
{
    private readonly ILogger<MidiEndpointsHandler> _logger;
    private readonly IMidiDeviceService _midiDeviceService;
    private readonly IMidiDataService _midiDataService;

    public MidiEndpointsHandler(ILogger<MidiEndpointsHandler> logger, IMidiDeviceService midiDeviceService, IMidiDataService midiDataService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _midiDeviceService = midiDeviceService ?? throw new ArgumentNullException(nameof(midiDeviceService));
        _midiDataService = midiDataService ?? throw new ArgumentNullException(nameof(midiDataService));
    }

    public async Task<IResult> ResetDevice(string deviceName)
    {
        _logger.LogInformation("Resetting device {DeviceName}", deviceName);

        var midiDevice = await _midiDataService.GetDeviceByNameAsync(deviceName);
        var deviceEffects = midiDevice.DeviceEffects;
        bool errorResettingDevice = false;

        foreach (var effect in deviceEffects)
        {
            try
            {
                _logger.LogInformation("Resetting effect default active state {DefaultActive} for effect {Effect} on device {Device}", effect.DefaultActive, effect.Name, deviceName);

                var activateMessage = await _midiDataService.GetControlChangeMessageToActivateDeviceEffectAsync(deviceName, effect.Name, effect.DefaultActive);
                await _midiDeviceService.SendControlChangeMessageByDeviceMidiConnectNameAsync(midiDevice.MidiConnectName, activateMessage);
                _logger.LogInformation("Effect default state reset on device");

                try
                {
                    _logger.LogInformation("Resetting active state data for device");
                    await _midiDataService.UpdateDeviceEffectActiveStateAsync(deviceName, effect.Name, effect.DefaultActive);
                    _logger.LogInformation("Effect active state data updated");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error updating active state data for device {Device}", deviceName);
                    try
                    {
                        _logger.LogInformation("Reverting effect active state for effect {Effect} on device {Device}", 
                            effect.Name, 
                            deviceName);
                        errorResettingDevice = true;
                        var revertMessage = await _midiDataService.GetControlChangeMessageToActivateDeviceEffectAsync(
                            deviceName, effect.Name, effect.Active);
                        await _midiDeviceService.SendControlChangeMessageByDeviceMidiConnectNameAsync(
                            midiDevice.MidiConnectName, revertMessage);
                        _logger.LogInformation("Effect active state reverted on device");
                    }
                    catch (Exception exception)
                    {
                        _logger.LogError(
                            exception, 
                            "Error reverting effect active state for effect {Effect} on device {Device}: database and device out of sync", 
                            effect.Name, 
                            deviceName);

                        var problem = Results.Problem(
                            detail: $"Failed to revert effect '{effect.Name}' active state for device '{deviceName}', suspending reset operation: {exception.Message}",
                            title: "Device reset failed - inconsistent state",
                            statusCode: 500);

                        return problem;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error resetting default state on device {Device}", deviceName);
                errorResettingDevice = true;
            }

            foreach (var setting in effect.DeviceEffectSettings)
            {
                try
                {
                    await _midiDataService.GetControlChangeMessageToSetDeviceEffectSettingAsync(
                        deviceName, effect.Name, setting.Name, setting.DefaultValue);
                    //TODO: Send the setting message to the device
                }
                catch (Exception e)
                {
                    _logger.LogError(e, 
                        "Error getting setting message for setting {Setting} on effect {Effect} on device {Device}", 
                        setting.Name, effect.Name, deviceName);
                    errorResettingDevice = true;
                }
            }
        }

        if (errorResettingDevice)
        {
            return Results.Problem(
                detail: $"Errors occurred resetting device '{deviceName}', please check logs for details.",
                title: "Device reset completed with errors",
                statusCode: 500);
        }

        return Results.Ok(new { Message = $"Device '{deviceName}' reset to default settings successfully." });
    }
    
    public async Task<IResult> PostControlChangeMessageToDeviceByMidiConnectName(string deviceMidiConnectName, int controlChangeMessageAddress, int controlChangeMessageValue)
    {
        _logger.LogInformation("Received request to send Control Change Message to device {DeviceName}", deviceMidiConnectName);
        try
        {
            await _midiDeviceService.SendControlChangeMessageByDeviceMidiConnectNameAsync(
                deviceMidiConnectName, 
                new() { Address = controlChangeMessageAddress, Value = controlChangeMessageValue });
            var msg = $"Request to send Control Change Message to device '{deviceMidiConnectName}' processed successfully.";
            _logger.LogInformation(msg);
            return Results.Ok(new { Message = msg });
        }
        catch (Exception ex)
        {
            var msg = $"Failed to send Control Change Message '{controlChangeMessageValue}' to device '{deviceMidiConnectName}': {ex.Message}";
            _logger.LogError(ex, msg);
            return Results.Problem(
                detail: msg,
                title: "Send Control Change Message failed",
                statusCode: 500);
        }
    }
}

