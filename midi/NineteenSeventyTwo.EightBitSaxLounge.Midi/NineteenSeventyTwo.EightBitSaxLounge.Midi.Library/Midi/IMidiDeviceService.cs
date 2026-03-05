using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;

public interface IMidiDeviceService
{
    Task SendControlChangeMessageByDeviceMidiConnectNameAsync(string midiConnectName, ControlChangeMessage controlChangeMessage);
}