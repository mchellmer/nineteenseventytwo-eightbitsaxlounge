namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;

using System.Text.Json;
using System.Text.Json.Serialization;

public class DeviceEffectSetting
{
    public required string Name { get; set; }
    public required int Value { get; set;  }
}