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

    public SetEffectHandler(
        ILogger<SetEffectHandler> logger,
        IMidiDataService midiDataService,
        IMidiDeviceService midiDeviceService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _midiDataService = midiDataService ?? throw new ArgumentNullException(nameof(midiDataService));
        _midiDeviceService = midiDeviceService ?? throw new ArgumentNullException(nameof(midiDeviceService));
    }

    public async Task<IResult> HandleAsync(SetEffectRequest request)
    {
        _logger.LogInformation("Setting effect: Device={DeviceName}, Effect={EffectName}, Setting={SettingName}", 
            request.DeviceName, request.DeviceEffectName, request.DeviceEffectSettingName);

        try
        {
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
            
            ControlChangeMessage? ccMessage = null;
            if (request.Value.HasValue)
            {
                _logger.LogInformation("Setting effect by value: {Value}", request.Value.Value);
                try
                {
                    ccMessage = GetControlChangeMessageForEffectValue(device.MidiImplementation, request);
                }
                catch (ArgumentException ex)
                {
                    _logger.LogWarning(ex, "Midi implementation detail not found for {DeviceName}", request.DeviceName);
                    return Results.NotFound(new { Message = ex.Message });
                }
            }
            else if (request.Selection != null)
            {
                _logger.LogInformation("Setting effect by selection: {Selection}", request.Selection);
                try
                {
                    ccMessage = await GetControlChangeMessageForEffectSelection(device.MidiImplementation, request);
                }
                catch (ArgumentException ex)
                {
                    _logger.LogWarning(ex, "Midi implementation detail not found for {DeviceName}", request.DeviceName);
                    return Results.NotFound(new { Message = ex.Message });
                }
            }
            
            if (ccMessage == null)
            {
                return Results.BadRequest(new { Message = "Either Value or Selection must be provided." });
            }
            
            await _midiDeviceService.SendControlChangeMessageByDeviceMidiConnectNameAsync(device.MidiConnectName, ccMessage);
                
            deviceEffectSetting.Value = ccMessage.Value;
            await _midiDataService.UpdateDeviceByNameAsync(device.Name, device);

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

    private ControlChangeAddress GetControlChangeAddress(List<MidiConfiguration> midiImplementation, SetEffectRequest request)
    {
        var configuration = midiImplementation.FirstOrDefault(
            c => c.Name.Equals(request.DeviceEffectSettingName, StringComparison.OrdinalIgnoreCase));

        if (configuration == null)
        {
            var msg = $"Midi configuration '{request.DeviceEffectSettingName}' not found for device '{request.DeviceName}'.";
            _logger.LogError(msg);
            throw new ArgumentException(msg);
        }
        
        if (configuration.ControlChangeAddresses == null || !configuration.ControlChangeAddresses.Any())
        {
            var msg = $"No control change addresses defined in configuration '{request.DeviceEffectSettingName}' for device '{request.DeviceName}'.";
            _logger.LogError(msg);
            throw new ArgumentException(msg);
        }

        ControlChangeAddress? address = configuration.ControlChangeAddresses.FirstOrDefault(address =>
            address.Name.Equals(request.DeviceEffectName, StringComparison.OrdinalIgnoreCase));

        if (address == null)
        {
            var msg = $"Control change address for effect '{request.DeviceEffectName}' not found in configuration '{request.DeviceEffectSettingName}' for device '{request.DeviceName}'.";
            _logger.LogError(msg);
            throw new ArgumentException(msg);
        }
        
        return address;
    }

    private async Task<ControlChangeMessage> GetControlChangeMessageForEffectSelection(List<MidiConfiguration> midiImplementation, SetEffectRequest request)
    {
        if (request.Selection == null)
        {
            throw new ArgumentException("Selection must be provided for GetControlChangeMessageForEffectSelection.");
        }
        
        Selector selector = await _midiDataService.GetSelectorByNameAsync(request.DeviceEffectSettingName)
            ?? throw new ArgumentException($"Selector '{request.DeviceEffectSettingName}' not found.");
        
        MidiSelection? selection = selector.Selections.FirstOrDefault(
            selection => selection.Name.Equals(request.Selection, StringComparison.OrdinalIgnoreCase))
            ?? throw new ArgumentException($"Selection '{request.Selection}' not found in selector '{request.DeviceEffectSettingName}'.");
        
        ControlChangeAddress address = GetControlChangeAddress(midiImplementation, request);
        
        if (!selection.ControlChangeMessageValue.HasValue)
        {
            var msg = $"Control change message value not defined for selection '{request.Selection}' in selector '{request.DeviceEffectSettingName}'.";
            _logger.LogError(msg);
            throw new ArgumentException(msg);
        }
        
        ControlChangeMessage ccMessage = new ControlChangeMessage
        {
            Address = address.Value,
            Value = selection.ControlChangeMessageValue.Value
        };

        return ccMessage;
    }
    
    private ControlChangeMessage GetControlChangeMessageForEffectValue(List<MidiConfiguration> midiImplementation, SetEffectRequest request)
    {
        if (!request.Value.HasValue)
        {
            throw new ArgumentException("Value must be provided for GetControlChangeMessageForEffectValue.");
        }

        ControlChangeAddress address = GetControlChangeAddress(midiImplementation, request);
        
        int ccAddress = address.Value;
        int ccValue = request.Value.Value;
        
        ControlChangeMessage ccMessage = new ControlChangeMessage
        {
            Address = ccAddress,
            Value = ccValue
        };
        
        return ccMessage;
    }
}
