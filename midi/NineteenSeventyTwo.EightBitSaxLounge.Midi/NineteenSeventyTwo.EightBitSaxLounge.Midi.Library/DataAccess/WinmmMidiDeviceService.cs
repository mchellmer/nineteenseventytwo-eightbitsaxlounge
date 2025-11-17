using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;

public class WinmmMidiDeviceService : IMidiDeviceService
{
    public Task<ControlChangeMessage> SendControlChangeMessageByDeviceNameAsync(string deviceName, ControlChangeMessage controlChangeMessage)
    {
        throw new NotImplementedException();
    }
}