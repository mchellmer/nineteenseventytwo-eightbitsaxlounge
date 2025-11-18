using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Enums;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;

/// <summary>
/// Ventris Dual Reverb Effect Activator
/// There are two reverb engines (A and B) that can be activated individually or together.
/// </summary>
public class VentrisDualReverbActivator : IEffectActivator
{
    // Addresses/values are example placeholders; use real mapping from device config.
    private const int ControlChangeAddress = 50;
    private const DualSingleMode ControlChangeValueSoloReverbEngineA = DualSingleMode.ReverbEngineA;
    private const DualSingleMode ControlChangeValueSoloReverbEngineB = DualSingleMode.ReverbEngineB;
    private const DualSingleMode ControlChangeValueDual = DualSingleMode.DualModeParallel;

    public Task<ControlChangeMessage?> BuildActivationMessageAsync(MidiDevice device, string effectName, bool activate)
    {
        // find both effects by name (case-insensitive)
        var targetReverbEngineEffect = device.DeviceEffects.FirstOrDefault(e => string.Equals(e.Name, effectName, StringComparison.OrdinalIgnoreCase));
        
        // dependent effect is DeviceEffect with name != target effect name
        var dependentReverbEngineEffect = device.DeviceEffects
            .First(e => (string.Equals(e.Name, "ReverbEngineA", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(e.Name, "ReverbEngineB", StringComparison.OrdinalIgnoreCase)) &&
                                 !string.Equals(e.Name, effectName, StringComparison.OrdinalIgnoreCase));
        
        // If target or dependent effect not found, return null
        if (targetReverbEngineEffect == null)
        {
            return Task.FromResult<ControlChangeMessage?>(null);
        }

        // Determine new mask based on activation/deactivation logic
        DualSingleMode newMask;
        if (activate)
        {
            if (dependentReverbEngineEffect.Active)
            {
                newMask = ControlChangeValueDual; // Activate both engines
            }
            else
            {
                newMask = string.Equals(targetReverbEngineEffect.Name, "ReverbEngineA", StringComparison.OrdinalIgnoreCase)
                    ? ControlChangeValueSoloReverbEngineA
                    : ControlChangeValueSoloReverbEngineB; // Activate only target engine
            }
        }
        else
        {
            if (dependentReverbEngineEffect.Active)
            {
                newMask = string.Equals(dependentReverbEngineEffect.Name, "ReverbEngineA", StringComparison.OrdinalIgnoreCase)
                    ? ControlChangeValueSoloReverbEngineA
                    : ControlChangeValueSoloReverbEngineB; // Keep dependent engine active
            }
            else
            {
                // Both engines inactive; set to a default state (e.g., ReverbEngineA)
                newMask = ControlChangeValueSoloReverbEngineA;
            }
        }

        var msg = new ControlChangeMessage
        {
            Address = ControlChangeAddress,
            Value = (int)newMask
        };
        return Task.FromResult<ControlChangeMessage?>(msg);
    }
}