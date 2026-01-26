using Microsoft.AspNetCore.Mvc;

using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Handlers;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Models;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Endpoints;

public static class MidiEndpoints
{
    public static void AddMidiEndpoints(this WebApplication app)
    {
        // Device control endpoint - uses injected IMidiDeviceService (local or proxied)
        app.MapPost("api/Midi/SendControlChangeMessage",
                async (
                    MidiEndpointsHandler handler,
                    [FromBody] SendControlChangeMessageRequest request) =>
                    await handler.PostControlChangeMessageToDeviceByMidiConnectName(
                        request.DeviceMidiConnectName,
                        request.Address,
                        request.Value))
            .RequireAuthorization()
            .WithName("SendControlChangeMessage")
            .WithTags("Device");

        // Data endpoints can be added here
        // e.g., CRUD operations for reverb configs stored in CouchDB
        // app.MapGet("api/Midi/Configs", ...).WithTags("Data");
        // app.MapPost("api/Midi/Configs", ...).WithTags("Data");
    }
}