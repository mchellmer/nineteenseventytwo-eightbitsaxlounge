using Microsoft.Extensions.Logging;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models.Winmm;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;

public class WinmmMidiDeviceService : IMidiDeviceService
{
    private readonly ILogger<WinmmMidiDeviceService> _logger;
    
    public WinmmMidiDeviceService(ILogger<WinmmMidiDeviceService> logger)
    {
        _logger = logger;
    }
    
    public async Task SendControlChangeMessageByDeviceMidiConnectNameAsync(string midiConnectName, ControlChangeMessage controlChangeMessage)
    {
        _logger.LogInformation("Sending control change message to device {DeviceName}", midiConnectName);
        try
        {
            _logger.LogInformation("Retrieving from available MIDI output devices");
            var device = new MidiOutDevice(midiConnectName);
            var sent = await device.TrySendControlChangeMessageAsync((uint)controlChangeMessage.Address, (uint)controlChangeMessage.Value);
            if (!sent)
            {
                _logger.LogError("Failed to send control change message to device {DeviceName}", midiConnectName);
                throw new Exception($"Failed to send control change message to device {midiConnectName}");
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