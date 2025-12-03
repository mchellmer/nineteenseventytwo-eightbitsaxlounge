using System;
using System.Threading;
using System.Threading.Tasks;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models.Winmm;

public interface IMidiOutDevice : IDisposable
{
    (bool success, MmResult result, string? errorText) TryOpen();
    void Close();
    Task<(bool success, MmResult lastResult, string? errorText)> TrySendControlChangeMessageDetailedAsync(
        int address,
        int value,
        bool closeAfterSend = false,
        int maxRetries = 3,
        int retryDelayMs = 50,
        CancellationToken cancellationToken = default);
}
