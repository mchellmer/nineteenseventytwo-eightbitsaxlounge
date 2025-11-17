using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;

public interface IMidiDeviceService
{
    Task<ControlChangeMessage> SendControlChangeMessageByDeviceNameAsync(string deviceName, ControlChangeMessage controlChangeMessage);
}