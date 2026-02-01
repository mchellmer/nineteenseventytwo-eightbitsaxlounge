using System.Text.Json.Serialization;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;

public class MidiDevice
{
    [JsonPropertyName("_id")]
    public string? Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public bool Active { get; set; } = false;
    public required List<MidiConfiguration> MidiImplementation { get; set; }
    public required string MidiConnectName { get; set; }
    public required List<DeviceEffect> DeviceEffects { get; set; }
}