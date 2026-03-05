using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Handlers;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Models;
using Xunit;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.UnitTests.EndpointTests;

public class ResetsFeatureTests : TestBase
{
    [Fact]
    public async Task SetEffect_TriggersResets_UpdatesDataStoreWithResetValues()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SetEffectHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();
        var deviceServiceMock = new Mock<IMidiDeviceService>();

        var deviceName = "TestDevice";
        var effectName = "TestEffect";
        var triggerSettingName = "TriggerSetting";
        var resetSettingName = "ResetSetting";

        var device = new MidiDevice
        {
            Name = deviceName,
            Description = "Desc",
            MidiConnectName = "Conn",
            MidiImplementation = new List<MidiConfiguration>
            {
                new MidiConfiguration
                {
                    Name = triggerSettingName,
                    ControlChangeAddresses = new List<ControlChangeAddress>
                    {
                        new ControlChangeAddress { Name = effectName, Value = 1 }
                    }
                }
            },
            DeviceEffects = new List<DeviceEffect>
            {
                new DeviceEffect
                {
                    Name = effectName,
                    Active = true,
                    EffectSettings = new List<DeviceEffectSetting>
                    {
                        new DeviceEffectSetting
                        {
                            Name = triggerSettingName,
                            Value = 0,
                            DefaultValue = 0,
                            Resets = new List<string> { resetSettingName }
                        },
                        new DeviceEffectSetting
                        {
                            Name = resetSettingName,
                            Value = 100, // Current value is NOT default
                            DefaultValue = 10 // Default value
                        }
                    }
                }
            }
        };

        dataServiceMock.Setup(m => m.GetDeviceByNameAsync(deviceName)).ReturnsAsync(device);
        dataServiceMock.Setup(m => m.GetControlChangeMessageToSetDeviceEffectSettingAsync(deviceName, effectName, triggerSettingName, 50))
            .ReturnsAsync(new ControlChangeMessage { Address = 1, Value = 50 });

        var handler = new SetEffectHandler(loggerMock.Object, dataServiceMock.Object, deviceServiceMock.Object);
        var request = new SetEffectRequest(deviceName, effectName, triggerSettingName, Value: 50);

        // Act
        var result = await handler.HandleAsync(request);
        var (status, _) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(200, status);

        // Verify MIDI was sent ONLY for the trigger setting
        deviceServiceMock.Verify(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync("Conn", It.Is<ControlChangeMessage>(msg => msg.Address == 1 && msg.Value == 50)), Times.Once);
        deviceServiceMock.Verify(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync(It.IsAny<string>(), It.IsAny<ControlChangeMessage>()), Times.Once);

        // Verify data store was updated with BOTH the new value for trigger AND the reset value for the other setting
        dataServiceMock.Verify(m => m.UpdateDeviceByNameAsync(deviceName, It.Is<MidiDevice>(d =>
            d.DeviceEffects[0].EffectSettings[0].Name == triggerSettingName && d.DeviceEffects[0].EffectSettings[0].Value == 50 &&
            d.DeviceEffects[0].EffectSettings[1].Name == resetSettingName && d.DeviceEffects[0].EffectSettings[1].Value == 10
        )), Times.Once);
    }

    [Fact]
    public async Task ResetDevice_OnlySendsCCForNonDefaultValues()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ResetDeviceHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();
        var deviceServiceMock = new Mock<IMidiDeviceService>();

        var deviceName = "TestDevice";
        var effectName = "TestEffect";
        var setting1Name = "Setting1";
        var setting2Name = "Setting2";

        var device = new MidiDevice
        {
            Name = deviceName,
            Description = "Desc",
            MidiConnectName = "Conn",
            MidiImplementation = new List<MidiConfiguration>
            {
                new MidiConfiguration
                {
                    Name = setting1Name,
                    ControlChangeAddresses = new List<ControlChangeAddress>
                    {
                        new ControlChangeAddress { Name = effectName, Value = 1 }
                    }
                },
                new MidiConfiguration
                {
                    Name = setting2Name,
                    ControlChangeAddresses = new List<ControlChangeAddress>
                    {
                        new ControlChangeAddress { Name = effectName, Value = 2 }
                    }
                }
            },
            DeviceEffects = new List<DeviceEffect>
            {
                new DeviceEffect
                {
                    Name = effectName,
                    Active = true,
                    DefaultActive = true,
                    EffectSettings = new List<DeviceEffectSetting>
                    {
                        new DeviceEffectSetting
                        {
                            Name = setting1Name,
                            Value = 50, // NOT default
                            DefaultValue = 0
                        },
                        new DeviceEffectSetting
                        {
                            Name = setting2Name,
                            Value = 0, // IS default
                            DefaultValue = 0
                        }
                    }
                }
            }
        };

        dataServiceMock.Setup(m => m.GetDeviceByNameAsync(deviceName)).ReturnsAsync(device);
        dataServiceMock.Setup(m => m.GetControlChangeMessageToActivateDeviceEffectAsync(deviceName, effectName, true))
            .ReturnsAsync(new ControlChangeMessage { Address = 100, Value = 127 });
        dataServiceMock.Setup(m => m.GetControlChangeMessageToSetDeviceEffectSettingAsync(deviceName, effectName, setting1Name, 0))
            .ReturnsAsync(new ControlChangeMessage { Address = 1, Value = 0 });

        var handler = new ResetDeviceHandler(loggerMock.Object, deviceServiceMock.Object, dataServiceMock.Object);

        // Act
        var result = await handler.HandleAsync(deviceName);
        var (status, _) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(200, status);

        // Should NOT send CC for effect activation since it's already at default
        deviceServiceMock.Verify(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync("Conn", It.Is<ControlChangeMessage>(msg => msg.Address == 100)), Times.Never);

        // Should send CC for setting1 (which was changed)
        deviceServiceMock.Verify(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync("Conn", It.Is<ControlChangeMessage>(msg => msg.Address == 1 && msg.Value == 0)), Times.Once);

        // Should NOT send CC for setting2 (which was already at default)
        deviceServiceMock.Verify(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync("Conn", It.Is<ControlChangeMessage>(msg => msg.Address == 2)), Times.Never);
    }
}
