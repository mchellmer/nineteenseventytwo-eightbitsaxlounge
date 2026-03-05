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
                    SendControlChangeMessageHandler handler,
                    [FromBody] SendControlChangeMessageRequest request) =>
                    await handler.HandleAsync(request))
            .RequireAuthorization()
            .WithName("SendControlChangeMessage")
            .WithTags("Device");

        // Data endpoints can be added here
        app.MapPost("api/Midi/InitDatamodel",
                async (InitializeDataModelHandler handler) =>
                    await handler.HandleAsync())
            .RequireAuthorization()
            .WithName("InitializeDataModel")
            .WithTags("Data");

        app.MapPost("api/Midi/UploadEffects",
            async (UploadEffectsHandler handler) =>
                await handler.HandleAsync())
            .RequireAuthorization()
            .WithName("UploadEffects")
            .WithTags("Data");

        app.MapPost("api/Midi/UploadDevice/{deviceName}",
                async (string deviceName, UploadDeviceHandler handler) =>
                    await handler.HandleAsync(deviceName))
            .RequireAuthorization()
            .WithName("UploadDevice")
            .WithTags("Data");

        app.MapPost("api/Midi/SetEffect",
                async (
                    SetEffectHandler handler,
                    [FromBody] SetEffectRequest request) =>
                    await handler.HandleAsync(request))
            .RequireAuthorization()
            .WithName("SetEffect")
            .WithTags("Device");

        app.MapPost("api/Midi/ResetDevice/{deviceName}",
                async (string deviceName, ResetDeviceHandler handler) =>
                    await handler.HandleAsync(deviceName))
            .RequireAuthorization()
            .WithName("ResetDevice")
            .WithTags("Device");
    }
}
