using Microsoft.Extensions.Logging;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models.Winmm;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;

public class WinmmMidiDeviceService : IMidiDeviceService
{
    private readonly ILogger<WinmmMidiDeviceService> _logger;
    private readonly IMidiOutDeviceFactory _deviceFactory;
    
    public WinmmMidiDeviceService(ILogger<WinmmMidiDeviceService> logger, IMidiOutDeviceFactory deviceFactory)
    {
        _logger = logger;
        _deviceFactory = deviceFactory;
    }
    
    public async Task SendControlChangeMessageByDeviceMidiConnectNameAsync(string midiConnectName, ControlChangeMessage controlChangeMessage)
    {
        _logger.LogInformation("Sending control change message to device {DeviceName}", midiConnectName);
        try
        {
            _logger.LogInformation("Retrieving from available MIDI output devices");
            using var device = _deviceFactory.Create(midiConnectName);
            var (opened, openResult, openError) = device.TryOpen();
            if (!opened)
            {
                _logger.LogError("Failed to open device {DeviceName}: {Error} (MMResult: {Result})", midiConnectName, openError, openResult);
                throw new Exception($"Failed to open device {midiConnectName}: {openError} (MMResult: {openResult})");
            }
            (var success, var mmResult, var error) =
                await device.TrySendControlChangeMessageDetailedAsync(controlChangeMessage.Address, controlChangeMessage.Value, closeAfterSend: true).ConfigureAwait(false);
            if (!success)
            {
                _logger.LogError(
                    "Error sending control change message to device {DeviceName}: {Error} (MMResult: {MmResult})",
                    midiConnectName, error, mmResult);
                throw new Exception(
                    $"Error sending control change message to device {midiConnectName}: {error} (MMResult: {mmResult})");
            }
            _logger.LogInformation("Control change message sent to device {DeviceName}", midiConnectName);
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Error sending control change message to device {DeviceName}: {Message}", midiConnectName, e.Message);
            throw new Exception($"Error sending control change message to device {midiConnectName}: {e.Message}");
        }
    }
}