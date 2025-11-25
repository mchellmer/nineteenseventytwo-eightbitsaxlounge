namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Models;

/// <summary>
/// Configuration options for application authentication.
/// </summary>
public sealed class AppAuthenticationOptions
{
    public string Issuer { get; set; } = "";
    public string Audience { get; set; } = "";
    public string SecretKey { get; set; } = "";
    public int TokenExpiryInMinutes { get; set; } = 5;
    public List<ClientAuthenticationEntry> Clients { get; set; } = new();
}