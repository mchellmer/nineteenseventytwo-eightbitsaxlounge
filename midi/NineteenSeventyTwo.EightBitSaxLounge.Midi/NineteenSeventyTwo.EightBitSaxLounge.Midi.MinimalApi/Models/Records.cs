namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Models;

public record AuthenticationData(string? UserName, string? Password);
public record SendControlChangeMessageRequest(string DeviceMidiConnectName, int Address, int Value, int Channel = 0);
public record UserData(int Id, string FirstName, string LastName, string UserName);