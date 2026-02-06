using Microsoft.IdentityModel.Tokens;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Models;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Handlers;

public class SetEffectHandler : IEndpointHandler<SetEffectRequest, IResult>
{
    private readonly ILogger<SetEffectHandler> _logger;
    private readonly IMidiDataService _midiDataService;
    private readonly IMidiDeviceService _midiDeviceService;
    private readonly HandlerHelper _handlerHelper;

    public SetEffectHandler(
        ILogger<SetEffectHandler> logger,
        IMidiDataService midiDataService,
        IMidiDeviceService midiDeviceService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _midiDataService = midiDataService ?? throw new ArgumentNullException(nameof(midiDataService));
        _midiDeviceService = midiDeviceService ?? throw new ArgumentNullException(nameof(midiDeviceService));
        _handlerHelper = new HandlerHelper(_logger, _midiDeviceService, _midiDataService);
    }

    public async Task<IResult> HandleAsync(SetEffectRequest request)
    {
        _logger.LogInformation("Setting effect: Device={DeviceName}, Effect={EffectName}, Setting={SettingName}", 
            request.DeviceName, request.DeviceEffectName, request.DeviceEffectSettingName);

        try
        {
            // Get the Device, the named DeviceEffect, and the named DeviceEffectSetting
            MidiDevice? device = await _midiDataService.GetDeviceByNameAsync(request.DeviceName);
            if (device == null)
            {
                _logger.LogWarning("Device not found: {DeviceName}", request.DeviceName);
                return Results.NotFound(new { Message = $"Device '{request.DeviceName}' not found." });
            }

            DeviceEffect? deviceEffect = device.DeviceEffects.FirstOrDefault(e => e.Name.Equals(request.DeviceEffectName, StringComparison.OrdinalIgnoreCase));
            if (deviceEffect == null)
            {
                _logger.LogWarning("Effect not found: {EffectName} on device {DeviceName}", request.DeviceEffectName, request.DeviceName);
                return Results.NotFound(new { Message = $"Effect '{request.DeviceEffectName}' not found on device '{request.DeviceName}'." });
            }

            DeviceEffectSetting? deviceEffectSetting = deviceEffect.EffectSettings.FirstOrDefault(s => s.Name.Equals(request.DeviceEffectSettingName, StringComparison.OrdinalIgnoreCase));
            if (deviceEffectSetting == null)
            {
                _logger.LogWarning("Setting not found: {SettingName} for effect {EffectName}", request.DeviceEffectSettingName, request.DeviceEffectName);
                return Results.NotFound(new { Message = $"Setting '{request.DeviceEffectSettingName}' not found for effect '{request.DeviceEffectName}'." });
            }

            _logger.LogInformation("Found setting {SettingName} for effect {EffectName} on device {DeviceName}", 
                deviceEffectSetting.Name, deviceEffect.Name, device.Name);

            // Capture original value to support rollback if data persistence fails
            int originalValue = deviceEffectSetting.Value;
            
            ControlChangeMessage? ccMessage = null;
            try
            {
                if (request.Value.HasValue)
                {
                    _logger.LogInformation("Setting effect by value: {Value}", request.Value.Value);
                    ccMessage = await _midiDataService.GetControlChangeMessageToSetDeviceEffectSettingAsync(
                        request.DeviceName, request.DeviceEffectName, request.DeviceEffectSettingName, request.Value.Value);
                }
                else if (request.Selection != null)
                {
                    _logger.LogInformation("Setting effect by selection: {Selection}", request.Selection);
                    ccMessage = await _midiDataService.GetControlChangeMessageToSetDeviceEffectSettingSelectionAsync(
                        request.DeviceName, request.DeviceEffectName, request.DeviceEffectSettingName, request.Selection);
                }
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning(ex, "Failed to generate MIDI message for {DeviceName}", request.DeviceName);
                return Results.NotFound(new { Message = ex.Message });
            }
            
            if (ccMessage == null)
            {
                return Results.BadRequest(new { Message = "Either Value or Selection must be provided." });
            }
            
            try
            {
                await _handlerHelper.SendMessageAndUpdateDataWithRollbackAsync(
                    device,
                    ccMessage,
                    originalValue,
                    () => deviceEffectSetting.Value = ccMessage.Value,
                    () => deviceEffectSetting.Value = originalValue,
                    () => _midiDataService.UpdateDeviceByNameAsync(device.Name, device),
                    request.DeviceName);
            }
            catch (Exception)
            {
                return Results.Problem(
                    detail: "Persisting effect change failed; device state rolled back",
                    title: "Update failed",
                    statusCode: 500);
            }

            return Results.Ok(new
            {
                Message = $"Effect set for device {request.DeviceName} to {ccMessage.Value}"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to set effect for device {DeviceName}", request.DeviceName);
            return Results.Problem(
                detail: ex.Message,
                title: "Set effect failed",
                statusCode: 500);
        }
    }
}
