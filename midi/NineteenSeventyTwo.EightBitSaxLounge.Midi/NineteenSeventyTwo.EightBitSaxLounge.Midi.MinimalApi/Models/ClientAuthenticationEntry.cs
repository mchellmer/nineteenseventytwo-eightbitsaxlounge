namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Models;

/// <summary>
/// Represents a client authentication entry with a client ID and secret.
/// </summary>
public sealed class ClientAuthenticationEntry
{
    public string ClientId { get; set; } = "";
    public string ClientSecret { get; set; } = "";
}