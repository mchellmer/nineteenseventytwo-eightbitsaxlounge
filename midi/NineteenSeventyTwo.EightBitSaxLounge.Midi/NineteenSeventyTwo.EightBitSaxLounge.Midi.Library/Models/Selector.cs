using System.Text.Json.Serialization;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;

public class Selector
{
    [JsonPropertyName("_id")]
    public string? Id { get; set; }
    public required string Name { get; set; }
    public required List<MidiSelection> Selections { get; set; }
}