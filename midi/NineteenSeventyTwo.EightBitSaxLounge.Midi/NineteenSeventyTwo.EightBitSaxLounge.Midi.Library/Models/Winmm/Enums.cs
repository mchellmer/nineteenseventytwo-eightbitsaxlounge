namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models.Winmm;

/// <summary>
/// An enum representing the callback flags for MIDI operations.
/// </summary>
public enum MidiCallbackFlags
{
    /// <summary>
    /// No callback is used.
    /// </summary>
    NoCallBack = 0,

    /// <summary>
    /// The callback is a window handle.
    /// </summary>
    Window = 0x10000,

    /// <summary>
    /// The callback is a thread identifier.
    /// </summary>
    Thread = 0x20000,

    /// <summary>
    /// The callback is a function pointer.
    /// </summary>
    Function = 0x30000,

    /// <summary>
    /// The callback is an event handle.
    /// </summary>
    CallBackEvent = 0x50000,

    /// <summary>
    /// The callback is for MIDI input status.
    /// </summary>
    MidiIoStatus = 0x20
}

/// <summary>
/// An enum representing possible results for a midi message.
/// </summary>
/// <remarks>See mmresult winmm.dll documentation</remarks>
public enum MmResult
{
    NoError = 0,
    UnspecError = 1,
    BadDeviceId = 2,
    NotEnabled = 3,
    DeviceAlreadyAllocated = 4,
    InvalidHandle = 5,
    NoDriver = 6,
    NoMem = 7,
    NotSupported = 8,
    BadErrNum = 9,
    InvalidFlag = 10,
    InvalidParam = 11,
    HandleBusy = 12,
    InvalidAlias = 13,
    BadDb = 14,
    KeyNotFound = 15,
    ReadError = 16,
    WriteError = 17,
    DeleteError = 18,
    RegistryValueNotFound = 19,
    NoDriverCallback = 20,
    LastError = 20,
    MoreData = 21
}