using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Models;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Handlers;

public class SendControlChangeMessageHandler : IEndpointHandler<SendControlChangeMessageRequest, IResult>
{
    private readonly ILogger<SendControlChangeMessageHandler> _logger;
    private readonly IMidiDeviceService _midiDeviceService;

    public SendControlChangeMessageHandler(
        ILogger<SendControlChangeMessageHandler> logger,
        IMidiDeviceService midiDeviceService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _midiDeviceService = midiDeviceService ?? throw new ArgumentNullException(nameof(midiDeviceService));
    }

    public async Task<IResult> HandleAsync(SendControlChangeMessageRequest request)
    {
        _logger.LogInformation("Received request to send Control Change Message to device {DeviceName}", request.DeviceMidiConnectName);
        try
        {
            await _midiDeviceService.SendControlChangeMessageByDeviceMidiConnectNameAsync(
                request.DeviceMidiConnectName,
                new() { Address = request.Address, Value = request.Value });

            var msg = $"Request to send Control Change Message to device '{request.DeviceMidiConnectName}' processed successfully.";
            _logger.LogInformation(msg);
            return Results.Ok(new { Message = msg });
        }
        catch (Exception ex)
        {
            var msg = $"Failed to send Control Change Message '{request.Value}' to device '{request.DeviceMidiConnectName}': {ex.Message}";
            _logger.LogError(ex, msg);
            return Results.Problem(
                detail: msg,
                title: "Send Control Change Message failed",
                statusCode: 500);
        }
    }
}
