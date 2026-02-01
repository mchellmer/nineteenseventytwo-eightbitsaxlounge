namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;

public class DeviceSetting
{
    public required string Name { get; set; }
    public required string DeviceName { get; set; }
    public required string EffectName { get; set; }
    public string? DeviceMidiImplementationName { get; set; }
}