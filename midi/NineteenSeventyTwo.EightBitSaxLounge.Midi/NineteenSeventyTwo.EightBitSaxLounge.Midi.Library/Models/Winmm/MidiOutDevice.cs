using System.Runtime.InteropServices;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models.Winmm;

/// <summary>
/// A class representing a MIDI output device.
/// </summary>
public class MidiOutDevice : MidiDevice
{
    private const UInt32 ControlChangeMessageMask = 0x0101B0u;
    
    /// <summary>
    /// The identifier of the MIDI output device.
    /// </summary>
    internal UInt32 DeviceId;
    
    /// <summary>
    /// The name of the MIDI output device.
    /// </summary>
    public string? DeviceName { get; set; }
    
    /// <summary>
    /// A handle to reference the device port in communications.
    /// </summary>
    internal IntPtr Handle;
    
    /// <summary>
    /// A flag indicating whether the MIDI output device is open
    /// </summary>
    public bool IsOpen;

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
    
    public override void Open() 
    {
        MmResult result;

        if (!IsOpen)
        {
            result = midiOutOpen(ref Handle, DeviceId, IntPtr.Zero, 0, MidiCallbackFlags.NoCallBack);

            if (result == MmResult.NoError)
                IsOpen = true;
        }
    }

    /// <summary>
    /// Initializes a new instance of the MidiOutDevice class with the specified MIDI connection name.
    /// </summary>
    /// <param name="midiConnectName">
    /// The friendly name of the MIDI output device to connect to.
    /// </param>
    public MidiOutDevice(string midiConnectName)
    {
        UInt32 numDevs = midiOutGetNumDevs();
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
                DeviceName = devName;
            }
        }
    }
    
    public override void Close() 
    {
        MmResult result;

        if (IsOpen)
        {
            result = midiOutClose(Handle);

            if (result == MmResult.NoError)
                IsOpen = false;
        }
    }

    public Task<bool> TrySendControlChangeMessageAsync(
        uint address,
        uint value,
        bool closeAfterSend = false,
        int maxRetries = 3,
        int retryDelayMs = 50,
        CancellationToken cancellationToken = default)
    {
        // Offload the synchronous WinMM calls to a background thread so callers can await.
        return Task.Run(() =>
        {
            Open();

            if (!IsOpen)
                return false;

            var msg = (value << 16) | (address << 8) | 0xB0u;
            MmResult result = MmResult.NoError;

            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();

                result = midiOutShortMsg(Handle, msg);
                if (result == MmResult.NoError)
                {
                    if (closeAfterSend)
                        Close();

                    return true;
                }

                // wait but observe cancellation
                if (attempt < maxRetries - 1)
                {
                    if (cancellationToken.WaitHandle.WaitOne(retryDelayMs))
                        throw new OperationCanceledException(cancellationToken);
                }
            }

            return false;
        }, cancellationToken);
    }
}