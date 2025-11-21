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
    
    public Task CreateDeviceAsync(MidiDevice newDevice)
    {
        throw new NotImplementedException();
    }

    public async Task<MidiDevice> GetDeviceByNameAsync(string deviceName)
    {
        _logger.LogInformation($"Retrieving device by name: {deviceName}");
        try
        {
            var response = await _eightBitSaxLoungeMidiDataAccess.LoadDataAsync<MidiDevice, EightBitSaxLoungeDataRequest>(
                "GET",
                new EightBitSaxLoungeDataRequest
                {
                    RequestRoute = "devices/docs",
                    RequestBody = null
                },
                DataLayerConnectionStringName);
            
            var deviceNameMatch = response.Where(d => d.Name == deviceName).ToList();
            
            if (deviceNameMatch.Count > 1)
            {
                _logger.LogError($"Multiple devices found with name {deviceName}");
                throw new InvalidOperationException($"Multiple devices found with name {deviceName}");
            }

            if (deviceNameMatch.Count != 1)
            {
                _logger.LogError($"Device with name {deviceName} does not exist");
                throw new InvalidOperationException($"Device with name {deviceName} does not exist");
            }

            _logger.LogInformation($"Device with name {deviceName} retrieved successfully");
            return deviceNameMatch.First();
        }
        catch (Exception e)
        {
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
    
    //TODO: couchdb device will return id and rev, need to handle that in update
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

    public Task CreateEffectAsync(string effectName, Effect newEffect)
    {
        throw new NotImplementedException();
    }

    public Task<Effect> GetEffectByNameAsync(string effectName)
    {
        throw new NotImplementedException();
    }

    public Task UpdateEffectByNameAsync(string effectName, Effect updatedEffect)
    {
        throw new NotImplementedException();
    }

    public Task DeleteEffectByNameAsync(string effectName)
    {
        throw new NotImplementedException();
    }

    public Task CreateSelectorAsync(string selectorName, Selector newSelector)
    {
        throw new NotImplementedException();
    }

    public Task<Selector> GetSelectorByNameAsync(string selectorName)
    {
        throw new NotImplementedException();
    }

    public Task UpdateSelectorByNameAsync(string selectorName, Selector updatedSelector)
    {
        throw new NotImplementedException();
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

    public Task<ControlChangeMessage> GetControlChangeMessageToSetDeviceEffectSettingAsync(
        string deviceName, string effectName, string settingName, int settingValue)
    {
        throw new NotImplementedException();
    }
}