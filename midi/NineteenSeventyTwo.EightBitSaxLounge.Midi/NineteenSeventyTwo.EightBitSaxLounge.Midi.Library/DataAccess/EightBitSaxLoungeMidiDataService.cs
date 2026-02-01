using Microsoft.Extensions.Logging;

using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;

public class EightBitSaxLoungeMidiDataService : IMidiDataService
{
    private const string DataLayerConnectionStringName = "EightBitSaxLoungeDataLayer";
    
    private readonly IEffectActivatorFactory _effectActivatorFactory;
    private readonly IDataAccess _eightBitSaxLoungeMidiDataAccess;
    private readonly ILogger<EightBitSaxLoungeMidiDataService> _logger;
    
    public EightBitSaxLoungeMidiDataService(
        IEffectActivatorFactory effectActivatorFactory,
        IDataAccess eightBitSaxLoungeMidiDataAccess,
        ILogger<EightBitSaxLoungeMidiDataService> logger)
    {
        _effectActivatorFactory = effectActivatorFactory;
        _eightBitSaxLoungeMidiDataAccess = eightBitSaxLoungeMidiDataAccess;
        _logger = logger;
    }
    
    public async Task CreateDatabaseAsync(string databaseName)
    {
        _logger.LogInformation($"Creating database: {databaseName}");
        try
        {
            await _eightBitSaxLoungeMidiDataAccess.SaveDataAsync(
                "PUT",
                new EightBitSaxLoungeDataRequest
                {
                    RequestRoute = databaseName,
                    RequestBody = null
                },
                DataLayerConnectionStringName);
            _logger.LogInformation($"Database created: {databaseName}");
        }
        catch (Exception e)
        {
            _logger.LogError($"Error creating database {databaseName}: {e.Message}");
            throw new Exception($"Error creating database {databaseName}: {e.Message}");
        }
    }
    
    public async Task CreateDeviceAsync(MidiDevice newDevice)
    {
        _logger.LogInformation($"Creating device: {newDevice.Name}");
        try
        {
            newDevice.Id = newDevice.Name;
            await _eightBitSaxLoungeMidiDataAccess.SaveDataAsync(
                "POST",
                new EightBitSaxLoungeDataRequest
                {
                    RequestRoute = "devices",
                    RequestBody = newDevice
                },
                DataLayerConnectionStringName);
            _logger.LogInformation($"Device created: {newDevice.Name}");
        }
        catch (Exception e)
        {
            _logger.LogError($"Error creating device {newDevice.Name}: {e.Message}");
            throw new Exception($"Error creating device {newDevice.Name}: {e.Message}");
        }
    }

    public async Task<MidiDevice?> GetDeviceByNameAsync(string deviceName)
    {
        _logger.LogInformation($"Retrieving device by name: {deviceName}");
        try
        {
            var response = await _eightBitSaxLoungeMidiDataAccess.LoadDataAsync<MidiDevice, EightBitSaxLoungeDataRequest>(
                "GET",
                new EightBitSaxLoungeDataRequest
                {
                    RequestRoute = $"devices/{deviceName}",
                    RequestBody = null
                },
                DataLayerConnectionStringName);
            
            if (response.Count > 1)
            {
                _logger.LogError($"Multiple devices found with name {deviceName}");
                throw new InvalidOperationException($"Multiple devices found with name {deviceName}");
            }

            if (response.Count == 0)
            {
                _logger.LogWarning($"Device with name {deviceName} does not exist");
                return null;
            }

            _logger.LogInformation($"Device with name {deviceName} retrieved successfully");
            return response.First();
        }
        catch (Exception e)
        {
            if (e.Message.Contains("404"))
            {
                _logger.LogWarning($"Device with name {deviceName} does not exist (404)");
                return null;
            }
            _logger.LogError($"Error retrieving device {deviceName}: {e.Message}");
            throw new Exception($"Error retrieving device {deviceName}: {e.Message}");
        }
    }

    public async Task UpdateDeviceByNameAsync(string deviceName, MidiDevice updatedDevice)
    {
        _logger.LogInformation($"Updating device by name: {deviceName}");
        
        try
        {
            await _eightBitSaxLoungeMidiDataAccess.SaveDataAsync(
                "PUT",
                new EightBitSaxLoungeDataRequest
                {
                    RequestRoute = $"devices/{updatedDevice.Name}",
                    RequestBody = updatedDevice
                },
                DataLayerConnectionStringName);
        }
        catch (Exception e)
        {
            _logger.LogError($"Error updating device {deviceName}: {e.Message}");
            throw new Exception($"Error updating device {deviceName}: {e.Message}");
        }
    }
    
    public async Task UpdateDeviceEffectActiveStateAsync(string deviceName, string effectName, bool activate)
    {
        _logger.LogInformation(
            $"Updating device effect active state: Device={deviceName}, Effect={effectName}, Activate={activate}");

        try
        {
            var device = await GetDeviceByNameAsync(deviceName);
        
            device.DeviceEffects.First(e => e.Name == effectName).Active = activate;
        
            await UpdateDeviceByNameAsync(deviceName, device);
        }
        catch (Exception e)
        {
            _logger.LogError(
                $"Error updating device effect active state for device {deviceName}, effect {effectName}: {e.Message}");
            throw new Exception(
                $"Error updating device effect active state for device {deviceName}, effect {effectName}: {e.Message}");
        }
    }

    public Task DeleteDeviceByNameAsync(string deviceName)
    {
        throw new NotImplementedException();
    }

    public async Task CreateEffectAsync(string effectName, Effect newEffect)
    {
        _logger.LogInformation($"Creating effect: {effectName}");
        try
        {
            newEffect.Id = effectName;
            await _eightBitSaxLoungeMidiDataAccess.SaveDataAsync(
                "POST",
                new EightBitSaxLoungeDataRequest
                {
                    RequestRoute = "effects",
                    RequestBody = newEffect
                },
                DataLayerConnectionStringName);
            _logger.LogInformation($"Effect created: {effectName}");
        }
        catch (Exception e)
        {
            _logger.LogError($"Error creating effect {effectName}: {e.Message}");
            throw new Exception($"Error creating effect {effectName}: {e.Message}");
        }
    }

    public async Task<Effect?> GetEffectByNameAsync(string effectName)
    {
        _logger.LogInformation($"Retrieving effect by name: {effectName}");
        try
        {
            var response = await _eightBitSaxLoungeMidiDataAccess.LoadDataAsync<Effect, EightBitSaxLoungeDataRequest>(
                "GET",
                new EightBitSaxLoungeDataRequest
                {
                    RequestRoute = $"effects/{effectName}",
                    RequestBody = null
                },
                DataLayerConnectionStringName);

            if (response.Count == 0)
            {
                _logger.LogWarning($"Effect with name {effectName} does not exist.");
                return null;
            }
            
            _logger.LogInformation($"Effect with name {effectName} retrieved successfully.");
            return response.First();
        }
        catch (Exception e)
        {
            if (e.Message.Contains("404"))
            {
                _logger.LogWarning($"Effect with name {effectName} does not exist (404).");
                return null;
            }
            _logger.LogError($"Error retrieving effect {effectName}: {e.Message}");
            throw new Exception($"Error retrieving effect {effectName}: {e.Message}");
        }
    }

    public async Task<List<Effect>> GetAllEffectsAsync()
    {
        _logger.LogInformation("Retrieving all effects");
        try
        {
            var effects = await _eightBitSaxLoungeMidiDataAccess.LoadDataAsync<Effect, EightBitSaxLoungeDataRequest>(
                "GET",
                new EightBitSaxLoungeDataRequest
                {
                    RequestRoute = "effects/docs",
                    RequestBody = null
                },
                DataLayerConnectionStringName);
            _logger.LogInformation($"Retrieved {effects.Count} effects successfully.");
            return effects;
        }
        catch (Exception e)
        {
            _logger.LogError($"Error retrieving all effects: {e.Message}");
            throw new Exception($"Error retrieving all effects: {e.Message}");
        }
    }

    public async Task UpdateEffectByNameAsync(string effectName, Effect updatedEffect)
    {
        _logger.LogInformation($"Updating effect: {effectName}");
        try
        {
            await _eightBitSaxLoungeMidiDataAccess.SaveDataAsync(
                "PUT",
                new EightBitSaxLoungeDataRequest
                {
                    RequestRoute = $"effects/{effectName}",
                    RequestBody = updatedEffect
                },
                DataLayerConnectionStringName);
            _logger.LogInformation($"Effect updated: {effectName}");
        }
        catch (Exception e)
        {
            _logger.LogError($"Error updating effect {effectName}: {e.Message}");
            throw new Exception($"Error updating effect {effectName}: {e.Message}");
        }
    }

    public Task DeleteEffectByNameAsync(string effectName)
    {
        throw new NotImplementedException();
    }

    public async Task CreateSelectorAsync(string selectorName, Selector newSelector)
    {
        _logger.LogInformation($"Creating selector: {selectorName}");
        try
        {
            newSelector.Id = selectorName;
            await _eightBitSaxLoungeMidiDataAccess.SaveDataAsync(
                "POST",
                new EightBitSaxLoungeDataRequest
                {
                    RequestRoute = "selectors",
                    RequestBody = newSelector
                },
                DataLayerConnectionStringName);
            _logger.LogInformation($"Selector created: {selectorName}");
        }
        catch (Exception e)
        {
            _logger.LogError($"Error creating selector {selectorName}: {e.Message}");
            throw new Exception($"Error creating selector {selectorName}: {e.Message}");
        }
    }

    public async Task<Selector?> GetSelectorByNameAsync(string selectorName)
    {
        _logger.LogInformation($"Retrieving selector by name: {selectorName}");
        try
        {
            var response = await _eightBitSaxLoungeMidiDataAccess.LoadDataAsync<Selector, EightBitSaxLoungeDataRequest>(
                "GET",
                new EightBitSaxLoungeDataRequest
                {
                    RequestRoute = $"selectors/{selectorName}",
                    RequestBody = null
                },
                DataLayerConnectionStringName);
            
            if (response.Count == 0)
            {
                _logger.LogWarning($"Selector with name {selectorName} does not exist.");
                return null;
            }

            _logger.LogInformation($"Selector with name {selectorName} retrieved successfully.");
            return response.First();
        }
        catch (Exception e)
        {
            if (e.Message.Contains("404"))
            {
                _logger.LogWarning($"Selector with name {selectorName} does not exist (404).");
                return null;
            }
            _logger.LogError($"Error retrieving selector {selectorName}: {e.Message}");
            throw new Exception($"Error retrieving selector {selectorName}: {e.Message}");
        }
    }

    public async Task UpdateSelectorByNameAsync(string selectorName, Selector updatedSelector)
    {
        _logger.LogInformation($"Updating selector: {selectorName}");
        try
        {
            await _eightBitSaxLoungeMidiDataAccess.SaveDataAsync(
                "PUT",
                new EightBitSaxLoungeDataRequest
                {
                    RequestRoute = $"selectors/{selectorName}",
                    RequestBody = updatedSelector
                },
                DataLayerConnectionStringName);
            _logger.LogInformation($"Selector updated: {selectorName}");
        }
        catch (Exception e)
        {
            _logger.LogError($"Error updating selector {selectorName}: {e.Message}");
            throw new Exception($"Error updating selector {selectorName}: {e.Message}");
        }
    }

    public Task DeleteSelectorByNameAsync(string selectorName)
    {
        throw new NotImplementedException();
    }

    public async Task<ControlChangeMessage> GetControlChangeMessageToActivateDeviceEffectAsync(
        string deviceName, string effectName, bool activate)
    {
        var device = await GetDeviceByNameAsync(deviceName);

        var activator = _effectActivatorFactory.GetActivator(device.Name);
        if (activator == null)
        {
            var msg = $"No activator registered for device '{device.Name}'. Ensure an activator is registered in the factory (check key/casing).";
            _logger.LogError(msg);
            throw new InvalidOperationException(msg);
        }

        try
        {
            var msg = await activator.BuildActivationMessageAsync(device, effectName, activate);
            if (msg == null)
            {
                var nullMsg = $"Activator for device '{device.Name}' returned null activation message for effect '{effectName}'.";
                _logger.LogError(nullMsg);
                throw new InvalidOperationException(nullMsg);
            }

            return msg;
        }
        catch (Exception ex)
        {
            var err = $"Activator for device '{device.Name}' threw an exception while building activation message for effect '{effectName}': {ex.Message}";
            _logger.LogError(ex, err);
            throw new InvalidOperationException(err, ex);
        }
    }
    
    public async Task<ControlChangeMessage> GetControlChangeMessageToSetDeviceEffectSettingAsync(
        string deviceName, string effectName, string settingName, int settingValue)
    {
        var device = await GetDeviceByNameAsync(deviceName);
        var deviceEffect = device.DeviceEffects.FirstOrDefault(de => de.Name == effectName);
        if (deviceEffect == null)
        {
            var msg = $"No effect named '{effectName}' found in device '{deviceName}'.";
            _logger.LogError(msg);
            throw new InvalidOperationException(msg);
        }
        
        var deviceEffectSetting = deviceEffect.EffectSettings
            .FirstOrDefault(des => des.Name == settingName);
        if (deviceEffectSetting == null)
        {
            var msg = $"No setting named '{settingName}' found for effect '{effectName}' in device '{deviceName}'.";
            _logger.LogError(msg);
            throw new InvalidOperationException(msg);
        }
        
        var settingMidiImplementationName = await GetSettingMidiImplementationName(
            deviceName, settingName, deviceEffect, deviceEffectSetting);
        
        var settingMidiConfiguration = device.MidiImplementation
            .FirstOrDefault(configuration => configuration.Name == settingMidiImplementationName);
        if (settingMidiConfiguration == null)
        {
            var msg = $"No MIDI implementation found for setting '{settingName}' in device '{deviceName}'.";
            _logger.LogError(msg);
            throw new InvalidOperationException(msg);
        }

        var settingAddress = 
            (settingMidiConfiguration.ControlChangeAddresses ?? throw new InvalidOperationException
            ($"No MIDI implementation found for setting '{settingName}' in device '{deviceName}'."))
            .FirstOrDefault(address => address.Name == effectName);
        
        if (settingAddress == null)
        {
            var msg = $"No ControlChangeAddress found for effect '{effectName}' in setting '{settingName}' for device '{deviceName}'.";
            _logger.LogError(msg);
            throw new InvalidOperationException(msg);
        }
        
        //TODO: validate setting value
        return new ControlChangeMessage
        {
            Address = settingAddress.Value,
            Value = settingValue
        };
    }

    /// <summary>
    /// Gets the MIDI implementation name for a device effect setting, considering any dependencies.
    /// </summary>
    /// <param name="deviceName"></param>
    /// <param name="settingName"></param>
    /// <param name="deviceEffect"></param>
    /// <param name="deviceEffectSetting"></param>
    /// <returns></returns>
    /// <exception cref="InvalidOperationException"></exception>
    private async Task<string> GetSettingMidiImplementationName(string deviceName, string settingName, DeviceEffect deviceEffect,
        DeviceEffectSetting deviceEffectSetting)
    {
        string settingMidiImplementationName;
        // Some settings are dependent on others e.g. if a reverb engine is 'Room' then Control1 is 'Bass'
        // If the EffectSetting has a DeviceEffectSettingDependencyName, get that effect's ControlChangeAddress instead
        if (!string.IsNullOrEmpty(deviceEffectSetting.DeviceEffectSettingDependencyName))
        {
            var deviceEffectSettingDependency = deviceEffect.EffectSettings.FirstOrDefault(setting => setting.Name == deviceEffectSetting.DeviceEffectSettingDependencyName);
            if (deviceEffectSettingDependency == null)
            {
                var msg = $"No dependency found with name '{deviceEffectSetting.DeviceEffectSettingDependencyName}' for setting '{settingName}' in device '{deviceName}'.";
                _logger.LogError(msg);
                throw new InvalidOperationException(msg);
            }

            // Get the effect keyed to the dependentSetting from selectors
            var dependentSettingSelector = await GetSelectorByNameAsync(deviceEffectSettingDependency.Name);
            if (dependentSettingSelector == null)
            {
                var msg = $"No selector found with name '{deviceEffectSettingDependency.Name}' for dependency in device '{deviceName}'.";
                _logger.LogError(msg);
                throw new InvalidOperationException(msg);
            }

            var dependentEffectName = dependentSettingSelector.Selections.FirstOrDefault(
                selection => selection.ControlChangeMessageValue == deviceEffectSettingDependency.Value)?.Name;
            if (string.IsNullOrEmpty(dependentEffectName))
            {
                var msg = $"No dependent effect found for setting '{settingName}' in device '{deviceName}'.";
                _logger.LogError(msg);
                throw new InvalidOperationException(msg);
            }
            
            var dependentEffect = await GetEffectByNameAsync(dependentEffectName);
            if (dependentEffect.DeviceSettings == null)
            {
                var msg = $"No device settings found for dependent effect '{dependentEffectName}' in device '{deviceName}'.";
                _logger.LogError(msg);
                throw new InvalidOperationException(msg);
            }
            
            var effectDeviceSetting = dependentEffect.DeviceSettings.FirstOrDefault(
                setting => setting.DeviceName == deviceName && setting.Name == settingName);
            if (effectDeviceSetting == null)
            {
                var msg = $"No device setting found for dependent effect '{dependentEffectName}' in device '{deviceName}' with setting name '{settingName}'.";
                _logger.LogError(msg);
                throw new InvalidOperationException(msg);
            }
            
            settingMidiImplementationName = effectDeviceSetting.DeviceMidiImplementationName;
        }
        else
        {
            settingMidiImplementationName = settingName;
        }

        return settingMidiImplementationName;
    }
}