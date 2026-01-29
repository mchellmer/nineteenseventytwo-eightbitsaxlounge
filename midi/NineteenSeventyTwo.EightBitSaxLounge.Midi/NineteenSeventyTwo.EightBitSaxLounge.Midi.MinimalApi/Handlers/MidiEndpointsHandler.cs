using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;
using Microsoft.Extensions.Options;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Models;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Handlers;

public class MidiEndpointsHandler
{
    private readonly ILogger<MidiEndpointsHandler> _logger;
    private readonly IMidiDeviceService _midiDeviceService;
    private readonly IMidiDataService _midiDataService;
    private readonly IOptions<DatabaseOptions> _databaseOptions;
    private readonly IOptions<EffectsOptions> _effectsOptions;

    public MidiEndpointsHandler(
        ILogger<MidiEndpointsHandler> logger, 
        IMidiDeviceService midiDeviceService, 
        IMidiDataService midiDataService,
        IOptions<DatabaseOptions> databaseOptions,
        IOptions<EffectsOptions> effectsOptions)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _midiDeviceService = midiDeviceService ?? throw new ArgumentNullException(nameof(midiDeviceService));
        _midiDataService = midiDataService ?? throw new ArgumentNullException(nameof(midiDataService));
        _databaseOptions = databaseOptions ?? throw new ArgumentNullException(nameof(databaseOptions));
        _effectsOptions = effectsOptions ?? throw new ArgumentNullException(nameof(effectsOptions));
    }

    /// <summary>
    /// Initializes the MIDI data model by creating necessary databases.
    /// Lists databases from appsettings.json and creates them via the data service.
    /// </summary>
    public async Task<IResult> InitializeDataModel()
    {
        _logger.LogInformation("Initializing MIDI data model");
        
        var databases = _databaseOptions.Value.Names;
        
        foreach (var db in databases)
        {
            try
            {
                _logger.LogInformation("Initializing database: {Database}", db);
                await _midiDataService.CreateDatabaseAsync(db);
                _logger.LogInformation("Database initialized: {Database}", db);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Error initializing database: {Database}", db);
                return Results.Problem(
                    detail: $"Failed to initialize database '{db}': {e.Message}",
                    title: "Data model initialization failed",
                    statusCode: 500);
            }
        }
        
        return Results.Ok(new { Message = $"MIDI data model initialized successfully. Databases created: {string.Join(", ", databases)}" } );
    }
    
    /// <summary>
    /// Uploads effects from appsettings.Effects.json to the effects database.
    /// Gets all existing effects, then for each effect in config:
    /// - If exists, update it
    /// - If not exists, create it
    /// </summary>
    public async Task<IResult> UploadEffects()
    {
        _logger.LogInformation("Starting effects upload");
        
        var effectsFromConfig = _effectsOptions.Value.Effects;
        if (effectsFromConfig.Count == 0)
        {
            _logger.LogWarning("No effects found in configuration");
            return Results.Ok(new { Message = "No effects found in configuration to upload" });
        }

        _logger.LogInformation($"Found {effectsFromConfig.Count} effects in configuration");

        try
        {
            // Get all existing effects from database
            List<Effect> existingEffects;
            try
            {
                existingEffects = await _midiDataService.GetAllEffectsAsync();
                _logger.LogInformation($"Retrieved {existingEffects.Count} existing effects from database");
            }
            catch (Exception e)
            {
                _logger.LogWarning($"Could not retrieve existing effects (database may be empty): {e.Message}");
                existingEffects = new List<Effect>();
            }

            int created = 0;
            int updated = 0;
            int failed = 0;
            var errors = new List<string>();

            foreach (var effect in effectsFromConfig)
            {
                try
                {
                    var existingEffect = existingEffects.FirstOrDefault(e => 
                        e.Name.Equals(effect.Name, StringComparison.OrdinalIgnoreCase));

                    if (existingEffect != null)
                    {
                        _logger.LogInformation($"Updating effect: {effect.Name}");
                        await _midiDataService.UpdateEffectByNameAsync(effect.Name, effect);
                        updated++;
                        _logger.LogInformation($"Effect updated successfully: {effect.Name}");
                    }
                    else
                    {
                        _logger.LogInformation($"Creating effect: {effect.Name}");
                        await _midiDataService.CreateEffectAsync(effect.Name, effect);
                        created++;
                        _logger.LogInformation($"Effect created successfully: {effect.Name}");
                    }
                }
                catch (Exception e)
                {
                    failed++;
                    var errorMsg = $"Failed to process effect '{effect.Name}': {e.Message}";
                    _logger.LogError(e, errorMsg);
                    errors.Add(errorMsg);
                }
            }

            var summary = $"Effects upload completed. Created: {created}, Updated: {updated}, Failed: {failed}";
            _logger.LogInformation(summary);

            if (failed > 0)
            {
                return Results.Problem(
                    detail: $"{summary}. Errors: {string.Join("; ", errors)}",
                    title: "Effects upload completed with errors",
                    statusCode: 500);
            }

            return Results.Ok(new { Message = summary, Created = created, Updated = updated });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during effects upload");
            return Results.Problem(
                detail: $"Effects upload failed: {ex.Message}",
                title: "Effects upload failed",
                statusCode: 500);
        }
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

