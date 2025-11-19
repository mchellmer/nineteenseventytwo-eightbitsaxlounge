using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Handlers;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Endpoints;

public static class MidiEndpoints
{
    public static void AddMidiEndpoints(this WebApplication app)
    {
        // Register the reset endpoint to the handler resolved from DI; handler receives a typed ILogger via DI.
        app.MapPut("api/Midi/{deviceName}/reset",
                (MidiEndpointsHandler handler, string deviceName) => handler.ResetDevice(deviceName))
            .RequireAuthorization();
    }
}