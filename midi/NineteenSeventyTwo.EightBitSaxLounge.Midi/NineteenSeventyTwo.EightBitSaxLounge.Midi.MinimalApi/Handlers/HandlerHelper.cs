using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Handlers;

public class HandlerHelper
{
    private readonly ILogger _logger;
    private readonly IMidiDeviceService _midiDeviceService;
    private readonly IMidiDataService _midiDataService;

    public HandlerHelper(
        ILogger logger,
        IMidiDeviceService midiDeviceService,
        IMidiDataService midiDataService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _midiDeviceService = midiDeviceService ?? throw new ArgumentNullException(nameof(midiDeviceService));
        _midiDataService = midiDataService ?? throw new ArgumentNullException(nameof(midiDataService));
    }

    public async Task SendMessageAndUpdateDataWithRollbackAsync(
        MidiDevice device,
        ControlChangeMessage ccMessage,
        int originalValue,
        Action updateInMemoryModel,
        Action revertInMemoryModel,
        Func<Task> persistUpdateAsync,
        string contextName)
    {
        await _midiDeviceService.SendControlChangeMessageByDeviceMidiConnectNameAsync(device.MidiConnectName, ccMessage);

        updateInMemoryModel();

        try
        {
            await persistUpdateAsync();
            _logger.LogInformation("Successfully updated data store for {ContextName}", contextName);
        }
        catch (Exception dataEx)
        {
            _logger.LogError(dataEx, "Failed to update data store for {ContextName}. Rolling back device state.", contextName);

            try
            {
                var rollbackMessage = new ControlChangeMessage
                {
                    Address = ccMessage.Address,
                    Value = originalValue
                };
                await _midiDeviceService.SendControlChangeMessageByDeviceMidiConnectNameAsync(device.MidiConnectName, rollbackMessage);
                revertInMemoryModel();
                _logger.LogInformation("Rollback CC sent for {ContextName} to restore value {OriginalValue}", contextName, originalValue);
            }
            catch (Exception rollbackEx)
            {
                _logger.LogError(rollbackEx, "Rollback failed for {ContextName}", contextName);
            }

            throw;
        }
    }

    /// <summary>
    /// Scales a value in the 0-127 MIDI range to a 0..targetBase integer range (inclusive).
    /// If the input value is already within 0..targetBase it is returned unchanged (assumed to already be in target base).
    /// Example: ScaleFrom127ToBase(127, 10) => 10; ScaleFrom127ToBase(10, 10) => 10
    /// </summary>
    /// <param name="value">The source value (expected 0..127). Values outside this range will be clamped.</param>
    /// <param name="targetBase">The target maximum value (e.g. 10).</param>
    /// <returns>An integer in the range 0..targetBase.</returns>
    public int ScaleFrom127ToBase(int value, int targetBase = 10)
    {
        if (targetBase <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(targetBase), "targetBase must be greater than zero.");
        }

        // If the value already appears to be in the target range, return it unchanged.
        if (value >= 0 && value <= targetBase)
        {
            return value;
        }

        // Clamp input to expected MIDI CC range
        int clamped = Math.Max(0, Math.Min(127, value));

        // Perform a proportional scaling and round to nearest integer
        int scaled = (int)Math.Round(clamped * (targetBase / 127.0));

        // Ensure final value is within 0..targetBase bounds
        if (scaled < 0) scaled = 0;
        if (scaled > targetBase) scaled = targetBase;

        return scaled;
    }
}
