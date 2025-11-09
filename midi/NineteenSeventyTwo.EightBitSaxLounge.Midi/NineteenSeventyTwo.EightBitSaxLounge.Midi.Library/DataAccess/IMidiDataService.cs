namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;

public interface IMidiDataService
{
    Task<object> UpsertDeviceAsync(object updatedDevice);
}