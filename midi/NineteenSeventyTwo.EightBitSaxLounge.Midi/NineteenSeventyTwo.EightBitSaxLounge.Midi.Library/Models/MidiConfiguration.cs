namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;

public class MidiConfiguration
{
    public required string Name { get; set; }
    public List<ControlChangeAddress>? ControlChangeAddresses { get; set; }
    public int? ControlChangeValueDefault { get; set; }
    public int? ControlChangeValueMax { get; set; }
    public string? ControlChangeValueSelector { get; set; }
}