using Microsoft.AspNetCore.Mvc;

using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Handlers;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Models;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Endpoints;

public static class MidiEndpoints
{
    public static void AddMidiEndpoints(this WebApplication app)
    {
        app.MapPost("api/Midi/SendControlChangeMessage",
                async (MidiEndpointsHandler handler, [FromBody] SendControlChangeMessageRequest sendControlChangeMessageRequest) => 
                    await handler.PostControlChangeMessageToDeviceByMidiConnectName(
                        sendControlChangeMessageRequest.DeviceMidiConnectName,
                        sendControlChangeMessageRequest.Address,
                        sendControlChangeMessageRequest.Value))
                .RequireAuthorization();
    }
}