using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Models;

public class DeviceUploadWrapper
{
    public List<MidiDevice>? Devices { get; set; }
    public List<Effect>? Effects { get; set; }
    public List<Selector>? Selectors { get; set; }
}