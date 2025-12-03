namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;

public class Selector
{
    public required string Name { get; set; }
    public required List<MidiSelection> Selections { get; set; }
}