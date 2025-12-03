namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Models;

public sealed record ClientCredentialRequest(string ClientId, string ClientSecret);
public record SendControlChangeMessageRequest(string DeviceMidiConnectName, int Address, int Value, int Channel = 0);