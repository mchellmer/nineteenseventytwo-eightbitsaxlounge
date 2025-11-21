namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models.Winmm;

public abstract class MidiDevice
{
    public abstract void Open();    // 'abstract' means derived classes must implement this method
    public abstract void Close();
}