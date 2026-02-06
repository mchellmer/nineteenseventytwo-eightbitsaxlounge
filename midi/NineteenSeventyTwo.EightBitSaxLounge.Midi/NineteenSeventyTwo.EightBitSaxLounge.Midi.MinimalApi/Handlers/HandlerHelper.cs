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
}
