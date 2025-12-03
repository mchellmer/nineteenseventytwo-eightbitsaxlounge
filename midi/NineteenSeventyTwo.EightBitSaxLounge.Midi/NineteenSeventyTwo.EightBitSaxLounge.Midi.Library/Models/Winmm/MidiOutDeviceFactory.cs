namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models.Winmm;

public class MidiOutDeviceFactory : IMidiOutDeviceFactory
{
    public IMidiOutDevice Create(string midiConnectName) => (IMidiOutDevice)new MidiOutDevice(midiConnectName);
}
