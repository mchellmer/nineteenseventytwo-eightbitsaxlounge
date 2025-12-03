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
    private const int ControlChangeValueSoloReverbEngineA = 0;
    private const int ControlChangeValueSoloReverbEngineB = 1;
    private const int ControlChangeValueDual = 2;

    public Task<ControlChangeMessage?> BuildActivationMessageAsync(MidiDevice device, string effectName, bool activate)
    {
        var targetReverbEngineEffect = device.DeviceEffects.FirstOrDefault(
            e => string.Equals(e.Name, effectName, StringComparison.OrdinalIgnoreCase));
        
        var dependentReverbEngineEffect = device.DeviceEffects
            .First(e => (string.Equals(e.Name, "ReverbEngineA", StringComparison.OrdinalIgnoreCase) ||
                                  string.Equals(e.Name, "ReverbEngineB", StringComparison.OrdinalIgnoreCase)) &&
                                 !string.Equals(e.Name, effectName, StringComparison.OrdinalIgnoreCase));
        
        if (targetReverbEngineEffect == null)
        {
            return Task.FromResult<ControlChangeMessage?>(null);
        }

        int activateControlChangeValue;
        if (activate)
        {
            if (dependentReverbEngineEffect.Active)
            {
                activateControlChangeValue = ControlChangeValueDual;
            }
            else
            {
                activateControlChangeValue = string.Equals(targetReverbEngineEffect.Name, "ReverbEngineA", StringComparison.OrdinalIgnoreCase)
                    ? ControlChangeValueSoloReverbEngineA
                    : ControlChangeValueSoloReverbEngineB;
            }
        }
        else
        {
            if (dependentReverbEngineEffect.Active)
            {
                activateControlChangeValue = string.Equals(dependentReverbEngineEffect.Name, "ReverbEngineA", StringComparison.OrdinalIgnoreCase)
                    ? ControlChangeValueSoloReverbEngineA
                    : ControlChangeValueSoloReverbEngineB;
            }
            else
            {
                activateControlChangeValue = ControlChangeValueSoloReverbEngineA;
            }
        }

        var msg = new ControlChangeMessage
        {
            Address = ControlChangeAddress,
            Value = activateControlChangeValue
        };
        return Task.FromResult<ControlChangeMessage?>(msg);
    }
}