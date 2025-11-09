using Microsoft.AspNetCore.Authorization;

using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Endpoints;

public static class MidiEndpoints
{
    public static void AddMidiEndpoints(this WebApplication app)
    {
        app.MapPut("api/Midi/{deviceName}/reset", ResetDevice);
    }
    
    [Authorize]
    private static async Task<IResult> ResetDevice(IMidiDeviceService midiService, IMidiDataService db, string deviceName)
    {
        // Apply static defaults to the physical device (idempotent)
        var updatedDevice = await midiService.ResetToDefaultsAsync(deviceName);

        // Upsert associated DB documents to reflect the defaults
        var upsertResult = await db.UpsertDeviceAsync(updatedDevice);

        // Return the updated device (or more detailed status if you prefer)
        return Results.Ok(new { Device = updatedDevice, DbResult = upsertResult });
    }
}