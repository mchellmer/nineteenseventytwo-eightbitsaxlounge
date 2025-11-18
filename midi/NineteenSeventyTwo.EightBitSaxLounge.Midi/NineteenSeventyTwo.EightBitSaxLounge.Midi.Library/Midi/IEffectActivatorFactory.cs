namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;

public interface IEffectActivatorFactory
{
    IEffectActivator? GetActivator(string deviceName);
}