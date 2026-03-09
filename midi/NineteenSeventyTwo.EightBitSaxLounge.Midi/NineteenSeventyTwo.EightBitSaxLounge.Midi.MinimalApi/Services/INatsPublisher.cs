namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Services;

public interface INatsPublisher
{
    Task PublishAsync(string subject, string value);
}
