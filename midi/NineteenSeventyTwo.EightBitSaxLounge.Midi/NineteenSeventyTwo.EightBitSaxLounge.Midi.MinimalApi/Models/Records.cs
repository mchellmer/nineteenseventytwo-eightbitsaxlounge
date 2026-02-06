namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Models;

public sealed record ClientCredentialRequest(string ClientId, string ClientSecret);
public record SendControlChangeMessageRequest(string DeviceMidiConnectName, int Address, int Value, int Channel = 0);
public record SetEffectRequest(string DeviceName, string DeviceEffectName, string DeviceEffectSettingName, string? Selection = null, int? Value = null);