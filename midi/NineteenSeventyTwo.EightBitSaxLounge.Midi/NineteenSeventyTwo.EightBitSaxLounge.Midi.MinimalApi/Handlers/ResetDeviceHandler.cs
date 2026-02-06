using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Handlers;

public class ResetDeviceHandler : IEndpointHandler<string, IResult>
{
    private readonly ILogger<ResetDeviceHandler> _logger;
    private readonly IMidiDeviceService _midiDeviceService;
    private readonly IMidiDataService _midiDataService;
    private readonly HandlerHelper _handlerHelper;

    public ResetDeviceHandler(
        ILogger<ResetDeviceHandler> logger,
        IMidiDeviceService midiDeviceService,
        IMidiDataService midiDataService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _midiDeviceService = midiDeviceService ?? throw new ArgumentNullException(nameof(midiDeviceService));
        _midiDataService = midiDataService ?? throw new ArgumentNullException(nameof(midiDataService));
        _handlerHelper = new HandlerHelper(_logger, _midiDeviceService, _midiDataService);
    }

    public async Task<IResult> HandleAsync(string deviceName)
    {
        _logger.LogInformation("Resetting device {DeviceName}", deviceName);

        var midiDevice = await _midiDataService.GetDeviceByNameAsync(deviceName);
        if (midiDevice == null)
        {
            return Results.NotFound(new { Message = $"Device '{deviceName}' not found." });
        }

        var deviceEffects = midiDevice.DeviceEffects;
        bool errorResettingDevice = false;

        foreach (var effect in deviceEffects)
        {
            try
            {
                if (effect.Active == effect.DefaultActive)
                {
                    _logger.LogInformation("Effect {Effect} is already in default active state {DefaultActive}, skipping MIDI message", effect.Name, effect.DefaultActive);
                }
                else
                {
                    _logger.LogInformation("Resetting effect default active state {DefaultActive} for effect {Effect} on device {Device}", effect.DefaultActive, effect.Name, deviceName);

                    var activateMessage = await _midiDataService.GetControlChangeMessageToActivateDeviceEffectAsync(deviceName, effect.Name, effect.DefaultActive);
                    await _midiDeviceService.SendControlChangeMessageByDeviceMidiConnectNameAsync(midiDevice.MidiConnectName, activateMessage);
                    _logger.LogInformation("Effect default state reset on device");
                }

                try
                {
                    _logger.LogInformation("Resetting active state data for device {Device}", deviceName);
                    await _midiDataService.UpdateDeviceEffectActiveStateAsync(deviceName, effect.Name, effect.DefaultActive);
                    _logger.LogInformation("Effect active state data updated for {Device}", deviceName);
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

            foreach (var setting in effect.EffectSettings)
            {
                if (setting.Value == setting.DefaultValue)
                {
                    _logger.LogInformation("Setting {Setting} is already at default value {DefaultValue}, skipping MIDI message", setting.Name, setting.DefaultValue);
                    continue;
                }

                int originalValue = setting.Value;
                try
                {
                    _logger.LogInformation("Resetting setting {Setting} to default value {DefaultValue} for effect {Effect} on device {Device}", 
                        setting.Name, setting.DefaultValue, effect.Name, deviceName);
                    
                    var ccMessage = await _midiDataService.GetControlChangeMessageToSetDeviceEffectSettingAsync(
                        deviceName, effect.Name, setting.Name, setting.DefaultValue);

                    await _handlerHelper.SendMessageAndUpdateDataWithRollbackAsync(
                        midiDevice,
                        ccMessage,
                        originalValue,
                        () => setting.Value = setting.DefaultValue,
                        () => setting.Value = originalValue,
                        () => _midiDataService.UpdateDeviceByNameAsync(midiDevice.Name, midiDevice),
                        setting.Name);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, 
                        "Error resetting setting {Setting} on effect {Effect} on device {Device}", 
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
}
