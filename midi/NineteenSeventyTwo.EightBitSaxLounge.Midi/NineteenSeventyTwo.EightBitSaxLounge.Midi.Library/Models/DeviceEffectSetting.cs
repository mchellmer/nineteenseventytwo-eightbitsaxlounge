namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;

public class DeviceEffectSetting
{
    public required string Name { get; set; }
    public string? DeviceEffectSettingDependencyName { get; set; }
    public required int DefaultValue { get; set; }
    public required int Value { get; set;  }
}