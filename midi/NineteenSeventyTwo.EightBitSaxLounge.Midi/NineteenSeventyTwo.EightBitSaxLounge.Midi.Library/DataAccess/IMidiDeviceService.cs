using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;

public interface IMidiDeviceService
{ 
    Task SendControlChangeMessageByDeviceMidiConnectNameAsync(string midiConnectName, ControlChangeMessage controlChangeMessage);
}