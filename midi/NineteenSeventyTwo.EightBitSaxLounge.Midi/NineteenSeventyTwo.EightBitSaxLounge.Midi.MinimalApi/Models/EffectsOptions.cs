using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Models;

public class EffectsOptions
{
    public const string SectionName = "Effects";
    
    public List<Effect> Effects { get; set; } = new();
}
