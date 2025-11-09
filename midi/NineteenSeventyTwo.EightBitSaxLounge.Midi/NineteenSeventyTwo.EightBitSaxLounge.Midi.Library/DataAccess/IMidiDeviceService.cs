namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;

public interface IMidiDeviceService
{
    Task<object> ResetToDefaultsAsync(string deviceName);
}