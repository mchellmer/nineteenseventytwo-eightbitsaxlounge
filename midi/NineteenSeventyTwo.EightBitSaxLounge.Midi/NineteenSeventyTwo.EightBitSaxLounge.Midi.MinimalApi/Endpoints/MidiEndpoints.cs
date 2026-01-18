using Microsoft.AspNetCore.Mvc;

using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Handlers;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Models;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Services;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Endpoints;

public static class MidiEndpoints
{
    public static void AddMidiEndpoints(this WebApplication app)
    {
        // Device control endpoint - proxies to Windows PC service
        app.MapPost("api/Midi/SendControlChangeMessage",
                async (
                    MidiDeviceProxyService proxyService,
                    HttpContext context,
                    [FromBody] SendControlChangeMessageRequest request) =>
                {
                    var authHeader = context.Request.Headers.Authorization.FirstOrDefault();
                    return await proxyService.ProxyControlChangeMessage(
                        request.DeviceMidiConnectName,
                        request.Address.ToString(),
                        request.Value.ToString(),
                        authHeader);
                })
            .RequireAuthorization()
            .WithName("SendControlChangeMessage")
            .WithTags("Device");

        // Data endpoints can be added here
        // e.g., CRUD operations for reverb configs stored in CouchDB
        // app.MapGet("api/Midi/Configs", ...).WithTags("Data");
        // app.MapPost("api/Midi/Configs", ...).WithTags("Data");
    }
}