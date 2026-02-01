namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;

public class DeviceEffect
{
    public required string Name { get; set; }
    public required bool Active { get; set; }
    public bool DefaultActive { get; set; } = false;
    public required List<DeviceEffectSetting> EffectSettings { get; set; }
}