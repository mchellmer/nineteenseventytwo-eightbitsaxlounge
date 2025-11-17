using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;

public class EightBitSaxLoungeMidiDataService : IMidiDataService
{
    public Task CreateDeviceAsync(MidiDevice newDevice)
    {
        throw new NotImplementedException();
    }

    public Task<MidiDevice> GetDeviceByNameAsync(string deviceName)
    {
        throw new NotImplementedException();
    }

    public Task UpdateDeviceByNameAsync(string deviceName, MidiDevice updatedDevice)
    {
        throw new NotImplementedException();
    }

    public Task UpdateDeviceEffectActiveStateAsync(string deviceName, string effectName, bool activate)
    {
        throw new NotImplementedException();
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

    public Task<ControlChangeMessage> GetControlChangeMessageToActivateDeviceEffectAsync(string deviceName, string effectName, bool activate)
    {
        throw new NotImplementedException();
    }

    public Task<ControlChangeMessage> GetControlChangeMessageToSetDeviceEffectSettingAsync(string deviceName, string effectName, string settingName,
        int settingValue)
    {
        throw new NotImplementedException();
    }
}