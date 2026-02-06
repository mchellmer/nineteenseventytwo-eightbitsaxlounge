namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models.Winmm;

public abstract class WinmmMidiDevice
{
    public abstract void Open();    // 'abstract' means derived classes must implement this method
    public abstract void Close();
}