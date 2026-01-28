using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;

public interface IMidiDataService
{
    // Datamodel Management
    Task CreateDatabaseAsync(string databaseName);
    
    // Device Data Management
    Task CreateDeviceAsync(MidiDevice newDevice);
    Task<MidiDevice> GetDeviceByNameAsync(string deviceName);
    Task UpdateDeviceByNameAsync(string deviceName, MidiDevice updatedDevice);
    Task UpdateDeviceEffectActiveStateAsync(string deviceName, string effectName, bool activate);
    Task DeleteDeviceByNameAsync(string deviceName);
    
    // Effect Data Management
    Task CreateEffectAsync(string effectName, Effect newEffect);
    Task<Effect> GetEffectByNameAsync(string effectName);
    Task UpdateEffectByNameAsync(string effectName, Effect updatedEffect);
    Task DeleteEffectByNameAsync(string effectName);
    
    // Selector Data Management
    Task CreateSelectorAsync(string selectorName, Selector newSelector);
    Task<Selector> GetSelectorByNameAsync(string selectorName);
    Task UpdateSelectorByNameAsync(string selectorName, Selector updatedSelector);
    Task DeleteSelectorByNameAsync(string selectorName);
    
    // Control Change Message Data
    Task<ControlChangeMessage> GetControlChangeMessageToActivateDeviceEffectAsync(string deviceName, string effectName, bool activate);
    Task<ControlChangeMessage> GetControlChangeMessageToSetDeviceEffectSettingAsync(string deviceName, string effectName, string settingName, int settingValue);
}