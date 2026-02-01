using System.Text.Json.Serialization;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;

public class Effect
{
    [JsonPropertyName("_id")]
    public string? Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public List<DeviceSetting>? DeviceSettings { get; set; }
}