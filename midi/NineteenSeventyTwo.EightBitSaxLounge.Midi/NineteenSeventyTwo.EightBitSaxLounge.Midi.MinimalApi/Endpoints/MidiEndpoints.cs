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
        app.MapPost("api/Midi/InitDatamodel",
                async (MidiEndpointsHandler handler) =>
                    await handler.InitializeDataModel())
            .RequireAuthorization()
            .WithName("InitializeDataModel")
            .WithTags("Data");
        
        app.MapPost("api/Midi/UploadEffects",
            async (MidiEndpointsHandler handler) =>
                await handler.UploadEffects())
            .RequireAuthorization()
            .WithName("UploadEffects")
            .WithTags("Data");

        app.MapPost("api/Midi/UploadDevice/{deviceName}",
                async (string deviceName, MidiEndpointsHandler handler) =>
                    await handler.UploadDevice(deviceName))
            .RequireAuthorization()
            .WithName("UploadDevice")
            .WithTags("Data");
    }
}
