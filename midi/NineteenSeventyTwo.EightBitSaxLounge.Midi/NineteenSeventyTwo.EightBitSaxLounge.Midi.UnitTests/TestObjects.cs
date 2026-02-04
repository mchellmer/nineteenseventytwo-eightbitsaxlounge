using System.Collections.Generic;
using System.Linq;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.UnitTests;

public static class TestObjects
{
    // Canonical names used across tests
    public const string DefaultDeviceName = "TestDevice";
    public const string DefaultEffectName = "TestEffect";
    public const string DefaultSettingName = "TestSetting";

    // Prototypes to reuse where mutation is not required
    public static readonly DeviceEffectSetting PrototypeSetting = new()
    {
        Name = DefaultSettingName,
        DefaultValue = 0,
        Value = 0
    };

    public static readonly DeviceEffect PrototypeEffect = new()
    {
        Name = DefaultEffectName,
        Active = false,
        DefaultActive = false,
        EffectSettings = new List<DeviceEffectSetting> { Clone(PrototypeSetting) }
    };

    public static readonly MidiDevice PrototypeDevice = new()
    {
        Name = DefaultDeviceName,
        Description = "Desc",
        MidiConnectName = "Conn",
        MidiImplementation = new List<MidiConfiguration>(),
        DeviceEffects = new List<DeviceEffect> { Clone(PrototypeEffect) }
    };

    // Deep clone helpers for safe use in mutable tests
    public static MidiDevice Clone(MidiDevice src)
    {
        return new MidiDevice
        {
            Id = src.Id,
            Name = src.Name,
            Description = src.Description,
            Active = src.Active,
            MidiConnectName = src.MidiConnectName,
            MidiImplementation = src.MidiImplementation?.Select(m => new MidiConfiguration
            {
                Name = m.Name,
                ControlChangeAddresses = m.ControlChangeAddresses?.ToList(),
                ControlChangeValueSelector = m.ControlChangeValueSelector
            }).ToList() ?? new List<MidiConfiguration>(),
            DeviceEffects = src.DeviceEffects?.Select(Clone).ToList() ?? new List<DeviceEffect>()
        };
    }

    public static DeviceEffect Clone(DeviceEffect src)
    {
        return new DeviceEffect
        {
            Name = src.Name,
            Active = src.Active,
            DefaultActive = src.DefaultActive,
            EffectSettings = src.EffectSettings?.Select(Clone).ToList() ?? new List<DeviceEffectSetting>()
        };
    }

    public static DeviceEffectSetting Clone(DeviceEffectSetting src)
    {
        return new DeviceEffectSetting
        {
            Name = src.Name,
            DeviceEffectSettingDependencyName = src.DeviceEffectSettingDependencyName,
            DefaultValue = src.DefaultValue,
            Value = src.Value
        };
    }

    // Convenience builders used in many tests
    public static MidiDevice BuildDevice(string name,
        IEnumerable<DeviceEffect>? effects = null)
    {
        return new MidiDevice
        {
            Name = name,
            Description = PrototypeDevice.Description,
            MidiConnectName = PrototypeDevice.MidiConnectName,
            MidiImplementation = new List<MidiConfiguration>(),
            DeviceEffects = effects?.Select(Clone).ToList() ?? new List<DeviceEffect>()
        };
    }

    public static DeviceEffect BuildEffect(string name,
        bool active = false,
        IEnumerable<DeviceEffectSetting>? settings = null,
        bool? defaultActive = null)
    {
        return new DeviceEffect
        {
            Name = name,
            Active = active,
            DefaultActive = defaultActive ?? false,
            EffectSettings = settings?.Select(Clone).ToList() ?? new List<DeviceEffectSetting>()
        };
    }

    public static DeviceEffectSetting BuildSetting(string name,
        int value = 0,
        int defaultValue = 0,
        string? dependencyName = null)
    {
        return new DeviceEffectSetting
        {
            Name = name,
            Value = value,
            DefaultValue = defaultValue,
            DeviceEffectSettingDependencyName = dependencyName
        };
    }
}
