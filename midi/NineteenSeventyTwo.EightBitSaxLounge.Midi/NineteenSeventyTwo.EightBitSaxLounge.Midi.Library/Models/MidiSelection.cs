namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;

public class MidiSelection
{
    public required string Name { get; set; }
    public required int ControlChangeMessageValue { get; set; }
    public int? ControlChangeMessageMaximumValue { get; set; }
}