using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;
using System.Text.Json;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Models;

public class DeviceUploadWrapper
{
    public List<MidiDevice>? Devices { get; set; }
    public List<JsonElement>? Effects { get; set; }
    public List<Selector>? Selectors { get; set; }
}