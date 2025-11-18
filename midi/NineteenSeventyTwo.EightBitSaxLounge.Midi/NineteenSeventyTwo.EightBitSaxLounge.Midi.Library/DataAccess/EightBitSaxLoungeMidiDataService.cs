using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;

public class EightBitSaxLoungeMidiDataService : IMidiDataService
{
    private const string DataLayerConnectionStringName = "EightBitSaxLoungeDataLayer";
    
    private readonly IEffectActivatorFactory _effectActivatorFactory;
    private readonly IDataAccess _eightBitSaxLoungeMidiDataAccess;
    
    public EightBitSaxLoungeMidiDataService(
        IEffectActivatorFactory effectActivatorFactory,
        IDataAccess eightBitSaxLoungeMidiDataAccess)
    {
        _effectActivatorFactory = effectActivatorFactory;
        _eightBitSaxLoungeMidiDataAccess = eightBitSaxLoungeMidiDataAccess;
    }
    
    public Task CreateDeviceAsync(MidiDevice newDevice)
    {
        throw new NotImplementedException();
    }

    public async Task<MidiDevice> GetDeviceByNameAsync(string deviceName)
    {
        try
        {
            var response = await _eightBitSaxLoungeMidiDataAccess.LoadDataAsync<CouchDbMidiDevice, EightBitSaxLoungeDataRequest>(
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
                throw new InvalidOperationException($"Multiple devices found with name {deviceName}");
            }

            if (deviceNameMatch.Count != 1)
            {
                throw new InvalidOperationException($"Device with name {deviceName} does not exist");
            }

            return deviceNameMatch.First();
        }
        catch (Exception e)
        {
            throw new Exception($"Error retrieving device {deviceName}: {e.Message}");
        }
    }

    public async Task UpdateDeviceByNameAsync(string deviceName, MidiDevice updatedDevice)
    {
        var couchDbDevice = updatedDevice as CouchDbMidiDevice;
        if (couchDbDevice == null)
        {
            throw new InvalidOperationException($"CouchDbMdiDevice with name not found: {deviceName}");
        }
        
        try
        {
            await _eightBitSaxLoungeMidiDataAccess.SaveDataAsync(
                "PUT",
                new EightBitSaxLoungeDataRequest
                {
                    RequestRoute = $"devices/{couchDbDevice.Id}",
                    RequestBody = couchDbDevice
                },
                DataLayerConnectionStringName);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }
    
    //TODO: couchdb device will return id and rev, need to handle that in update
    public async Task UpdateDeviceEffectActiveStateAsync(string deviceName, string effectName, bool activate)
    {
        var device = await GetDeviceByNameAsync(deviceName);
        
        device.DeviceEffects.First(e => e.Name == effectName).Active = activate;
        
        await UpdateDeviceByNameAsync(deviceName, device);
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
        if (activator != null)
        {
            var msg = await activator.BuildActivationMessageAsync(device, effectName, activate);
            if (msg != null) return msg;
        }
        
        throw new NotImplementedException(
            $"No activator found for device {deviceName} or unable to build activation message for effect {effectName}");
    }

    public Task<ControlChangeMessage> GetControlChangeMessageToSetDeviceEffectSettingAsync(
        string deviceName, string effectName, string settingName, int settingValue)
    {
        throw new NotImplementedException();
    }
}