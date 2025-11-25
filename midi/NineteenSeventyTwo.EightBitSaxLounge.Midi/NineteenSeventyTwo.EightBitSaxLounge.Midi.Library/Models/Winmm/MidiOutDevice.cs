namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models.Winmm;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;


/// <summary>
/// A class representing a MIDI output device.
/// Handles opening, closing, and sending messages to a midi capable device via winmm.dll.
/// </summary>
public class MidiOutDevice : MidiDevice, IMidiOutDevice
{
    /// <summary>
    /// The identifier of the MIDI output device.
    /// </summary>
    internal UInt32 DeviceId;

    /// <summary>
    /// A handle to reference the device port in communications.
    /// </summary>
    internal IntPtr Handle;
    
    /// <summary>
    /// A flag indicating whether the MIDI output device is open
    /// </summary>
    public bool IsOpen;
    
    /// <summary>
    /// A semaphore to ensure thread-safe sending of MIDI messages.
    /// Limit device access to one thread at a time.
    /// </summary>
    private readonly SemaphoreSlim _sendLock = new(1, 1);

    /// <summary>
    /// Closes the specified MIDI output device.
    /// </summary>
    /// <param name="handle">A handle to the MIDI output device to be closed.</param>
    /// <returns>
    /// An MmResult value indicating the result of the operation.
    /// </returns>
    /// <remarks>
    /// This method is defined in the external library winmm.dll.
    /// </remarks>
    [DllImport("winmm.dll")]
    internal static extern MmResult midiOutClose(IntPtr handle);
    
    /// <summary>
    /// Retrieves the capabilities of a specified MIDI output device.
    /// </summary>
    /// <param name="devId">The identifier of the MIDI output device.</param>
    /// <param name="devCapabilities">A reference to a MidiOutCaps structure to be filled with the device capabilities.</param>
    /// <param name="devCapsSize">The size, in bytes, of the MidiOutCaps structure.</param>
    /// <returns>
    /// An MmResult value indicating the result of the operation.
    /// </returns>
    /// <remarks>
    /// This method is defined in the external library winmm.dll.
    /// </remarks>
    [DllImport("winmm.dll", EntryPoint = "midiOutGetDevCaps")]
    private static extern MmResult midiOutGetDevCapsA(UInt32 devId, ref MidiOutCapabilities devCapabilities, UInt32 devCapsSize);
    
    /// <summary>
    /// Get number of MIDI output devices on the system.
    /// </summary>
    /// <returns>
    /// The number of MIDI output devices.
    /// </returns>
    /// <remarks>
    /// This method is defined in the external library winmm.dll.
    /// </remarks>
    [DllImport("winmm.dll")]
    private static extern UInt32 midiOutGetNumDevs();
    
    /// <summary>
    /// Opens the specified MIDI output device.
    /// </summary>
    /// <param name="handle">A reference to the handle that will be used to communicate with the MIDI output device.</param>
    /// <param name="devId">The identifier of the MIDI output device.</param>
    /// <param name="midiOutCallback">
    /// A callback function for MIDI output. Since a callback for MIDI 'OUT' will not be implemented,
    /// NULL should be passed in for the callback argument, which is done here with IntPtr.Zero.
    /// </param>
    /// <param name="instance">User instance data passed to the callback function. Not used in this case.</param>
    /// <param name="flags">Flags for opening the MIDI output device.</param>
    /// <returns>
    /// An MmResult value indicating the result of the operation.
    /// </returns>
    /// <remarks>
    /// This method is defined in the external library winmm.dll.
    /// </remarks>
    [DllImport("winmm.dll")]
    internal static extern MmResult midiOutOpen(
        ref IntPtr handle,
        UInt32 devId,
        IntPtr midiOutCallback,
        UInt32 instance,
        MidiCallbackFlags flags);
    
    /// <summary>
    /// Sends a short MIDI message to the specified MIDI output device.
    /// </summary>
    /// <param name="handle">A handle to the MIDI output device.</param>
    /// <param name="msg">
    /// The MIDI message to be sent. This is a 32-bit unsigned long in unmanaged C, which corresponds to .NET's System.UInt32.
    /// </param>
    /// <returns>
    /// An MmResult value indicating the result of the operation.
    /// </returns>
    /// <remarks>
    /// This method is defined in the external library winmm.dll.
    /// </remarks>
    /// <example>
    /// MMRESULT midiOutShortMsg( HMIDIOUT hmo,  DWORD      dwMsg);
    /// a 32-bit 'unsigned long' in unmanaged C which corresponds to .NET's System.UInt32
    /// </example>
    [DllImport("winmm.dll")]
    internal static extern MmResult midiOutShortMsg(IntPtr handle, UInt32 msg);

    /// <summary>
    /// Retrieves a textual description of the specified MIDI error code.
    /// </summary>
    /// <param name="mmrError">
    /// The MIDI error code for which to retrieve the description.
    /// </param>
    /// <param name="text">
    /// A StringBuilder to receive the error text.
    /// </param>
    /// <param name="cchText">
    /// The size, in characters, of the text buffer.
    /// </param>
    /// <returns>
    /// An MmResult value indicating the result of the operation.
    /// </returns>
    [DllImport("winmm.dll")]
    private static extern MmResult midiOutGetErrorText(MmResult mmrError, StringBuilder text, uint cchText);

    /// <summary>
    /// Initializes a new instance of the MidiOutDevice class with the specified MIDI connection name.
    /// </summary>
    /// <param name="midiConnectName">
    /// The friendly name of the MIDI output device to connect to.
    /// </param>
    public MidiOutDevice(string midiConnectName)
    {
        UInt32 numDevs = midiOutGetNumDevs();
        DeviceId = uint.MaxValue;
        MidiOutCapabilities outCapabilities = new MidiOutCapabilities();
        for (UInt32 dev = 0; dev < numDevs; ++dev)
        {
            MmResult res = midiOutGetDevCapsA(dev, ref outCapabilities, (UInt32)Marshal.SizeOf(outCapabilities));
            if (res != MmResult.NoError)
            {
                continue;
            }

            string devName = outCapabilities.deviceName.TrimEnd('\0');
            if (string.Equals(devName, midiConnectName, StringComparison.OrdinalIgnoreCase) ||
                devName.StartsWith(midiConnectName, StringComparison.OrdinalIgnoreCase))
            {
                DeviceId = dev;
                break;
            }
        }

        if (DeviceId == uint.MaxValue)
        {
            throw new InvalidOperationException($"MIDI device '{midiConnectName}' not found.");
        }
    }
    
    /// <summary>
    /// Closes the MIDI output device.
    /// </summary>
    public override void Close() 
    {
        MmResult result;

        if (IsOpen)
        {
            result = midiOutClose(Handle);

            if (result == MmResult.NoError)
            {
                IsOpen = false;
                Handle = IntPtr.Zero;
            }
        }
    }
    
    /// <summary>
    /// Opens the MIDI output device.
    /// </summary>
    public override void Open() 
    {
        TryOpen();
    }

    /// <summary>
    /// Tries to send a MIDI control change message to the device asynchronously, with detailed diagnostics and retry logic.
    /// </summary>
    /// <param name="address">
    /// The address of the control change message.
    /// </param>
    /// <param name="value">
    /// The value of the control change message.
    /// </param>
    /// <param name="closeAfterSend">
    /// Indicates whether to close the device after sending the message.
    /// </param>
    /// <param name="maxRetries">
    /// The maximum number of retry attempts if sending fails.
    /// </param>
    /// <param name="retryDelayMs">
    /// The delay in milliseconds between retry attempts.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains
    /// a tuple with three values: 
    /// - a boolean indicating if the message was sent successfully,
    /// - the last MmResult from the send operation,
    /// - an optional error message.
    /// </returns>
    public async Task<(bool success, MmResult lastResult, string? errorText)> TrySendControlChangeMessageDetailedAsync(
        int address,
        int value,
        bool closeAfterSend = false,
        int maxRetries = 3,
        int retryDelayMs = 50,
        CancellationToken cancellationToken = default)
    {
        if (address is < 0 or > 127) throw new ArgumentOutOfRangeException(nameof(address));
        if (value is < 0 or > 127) throw new ArgumentOutOfRangeException(nameof(value));

        await _sendLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (!IsOpen)
            {
                var (opened, openResult, openError) = TryOpen();
                if (!opened)
                {
                    return (false, openResult, openError ?? "Device failed to open.");
                }
            }

            uint msg = ((uint)value << 16) | ((uint)address << 8) | 0xB0u;
            MmResult last = MmResult.UnspecError;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                last = midiOutShortMsg(Handle, msg);
                if (last == MmResult.NoError)
                {
                    if (closeAfterSend) Close();
                    return (true, last, null);
                }

                if (attempt < maxRetries - 1)
                {
                    SafeReopen();
                    if (!IsOpen)
                    {
                        var (opened, openResult, openError) = TryOpen();
                        if (!opened)
                        {
                            return (false, openResult, openError ?? GetErrorText(last));
                        }
                    }
                    if (retryDelayMs > 0)
                    {
                        await Task.Delay(retryDelayMs, cancellationToken).ConfigureAwait(false);
                    }
                }
            }

            if (closeAfterSend) Close();
            return (false, last, GetErrorText(last));
        }
        finally
        {
            _sendLock.Release();
        }
    }

    private static string GetErrorText(MmResult result)
    {
        var sb = new StringBuilder(256);
        return midiOutGetErrorText(result, sb, (uint)sb.Capacity) == MmResult.NoError ? sb.ToString() : result.ToString();
    }

    private void SafeReopen()
    {
        Close();
        Handle = IntPtr.Zero;
        IsOpen = false;
        TryOpen();
    }

    public (bool success, MmResult result, string? errorText) TryOpen()
    {
        if (IsOpen) return (true, MmResult.NoError, null);
        MmResult result = midiOutOpen(ref Handle, DeviceId, IntPtr.Zero, 0, MidiCallbackFlags.NoCallBack);
        if (result == MmResult.NoError)
        {
            IsOpen = true;
            return (true, result, null);
        }
        Handle = IntPtr.Zero;
        IsOpen = false;
        return (false, result, GetErrorText(result));
    }

    /// <summary>
    /// Dispose the device
    /// </summary>
    public void Dispose()
    {
        Close();
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Finalizer for the MidiOutDevice class.
    /// </summary>
    ~MidiOutDevice()
    {
        Close();
    }
}