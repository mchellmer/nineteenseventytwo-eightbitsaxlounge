using System.Runtime.InteropServices;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models.Winmm;

/// <summary>
/// The MidiOutCaps struct is used to store the capabilities and characteristics of a MIDI output device.
/// The name MidiOutCaps is short for "MIDI Output Capabilities," which accurately describes its purpose.
/// </summary>
internal struct MidiOutCapabilities
{
    /// <summary>
    /// The identifier for the manufacturer of the device.
    /// </summary>
    public UInt16 manufacturerId;

    /// <summary>
    /// The identifier for the specific product.
    /// </summary>
    public UInt16 productId;

    /// <summary>
    /// The version of the driver for the device.
    /// </summary>
    public UInt32 driverVersion;

    /// <summary>
    /// The name of the device, stored as a fixed-length string.
    /// </summary>
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
    public String deviceName;

    /// <summary>
    /// The type of MIDI technology used by the device.
    /// </summary>
    public UInt16 wTechnology;

    /// <summary>
    /// The number of voices (sounds) the device can produce simultaneously.
    /// </summary>
    public UInt16 wSounds;

    /// <summary>
    /// The number of notes the device can play simultaneously.
    /// </summary>
    public UInt16 wNotes;

    /// <summary>
    /// The channels supported by the device.
    /// </summary>
    public UInt16 wChannelMask;

    /// <summary>
    /// The capabilities supported by the device, such as specific features or functions.
    /// </summary>
    public UInt32 support;
}