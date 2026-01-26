namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;

/// <summary>
/// Interface for services that can optionally proxy calls to a remote MIDI service.
/// Implementations can transparently route requests to either local or remote endpoints.
/// </summary>
public interface IMidiProxyService
{
    /// <summary>
    /// Gets whether this service is configured to proxy to a remote service.
    /// </summary>
    bool IsProxyEnabled { get; }

    /// <summary>
    /// Gets the remote service URL if proxy is enabled, null otherwise.
    /// </summary>
    string? ProxyUrl { get; }
}
