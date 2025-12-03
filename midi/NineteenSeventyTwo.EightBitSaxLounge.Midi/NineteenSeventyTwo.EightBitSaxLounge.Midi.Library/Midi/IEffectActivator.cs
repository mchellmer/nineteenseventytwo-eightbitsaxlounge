using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;

public interface IEffectActivator
{
    /// <summary>
    /// Build the ControlChangeMessage required to activate or deactivate the effect.
    /// </summary>
    Task<ControlChangeMessage?> BuildActivationMessageAsync(MidiDevice device, string effectName, bool activate);
}