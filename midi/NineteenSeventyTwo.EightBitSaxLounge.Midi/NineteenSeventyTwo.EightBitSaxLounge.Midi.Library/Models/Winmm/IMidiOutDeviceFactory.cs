namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models.Winmm;

public interface IMidiOutDeviceFactory
{
    IMidiOutDevice Create(string midiConnectName);
}
