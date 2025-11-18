namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;

/// <summary>
/// Factory to get the appropriate IEffectActivator for a given MIDI device.
/// All MIDI devices have different ways to activate/deactivate effects.
/// </summary>
public class EffectActivatorFactory : IEffectActivatorFactory
{
    private readonly Dictionary<string, IEffectActivator> _map;

    public EffectActivatorFactory(IEnumerable<IEffectActivator> activators)
    {
        // register by device name or device type key; inject activators via DI if desired
        _map = new Dictionary<string, IEffectActivator>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in activators)
        {
            if (a is VentrisDualReverbActivator) _map["VentrisDualReverb"] = a;
            // add other mappings here
        }
    }

    public IEffectActivator? GetActivator(string deviceName)
    {
        _map.TryGetValue(deviceName, out var activator);
        return activator;
    }
}