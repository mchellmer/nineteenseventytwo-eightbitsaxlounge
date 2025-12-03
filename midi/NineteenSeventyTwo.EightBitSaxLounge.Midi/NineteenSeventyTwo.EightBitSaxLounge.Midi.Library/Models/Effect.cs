namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;

public class Effect
{
    public required string Name { get; set; }
    public required string Description { get; set; }
    public List<DeviceSetting>? DeviceSettings { get; set; }
}