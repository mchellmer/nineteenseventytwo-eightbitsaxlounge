using Microsoft.Extensions.Logging;
using Moq;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Handlers;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Models;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.UnitTests.EndpointTests;

public class SetEffectHandlerTests : TestBase
{
    [Fact]
    public async Task SetEffect_ValidRequest_ReturnsOk()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SetEffectHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();
        var deviceServiceMock = new Mock<IMidiDeviceService>();

        var device = new MidiDevice
        {
            Name = "VentrisDualReverb",
            Description = "Desc",
            MidiConnectName = "Conn",
            MidiImplementation = new List<MidiConfiguration>
            {
                new MidiConfiguration
                {
                    Name = "Time",
                    ControlChangeAddresses = new List<ControlChangeAddress>
                    {
                        new ControlChangeAddress { Name = "ReverbEngineA", Value = 10 }
                    }
                }
            },
            DeviceEffects = new List<DeviceEffect>
            {
                new DeviceEffect
                {
                    Name = "ReverbEngineA",
                    Active = true,
                    EffectSettings = new List<DeviceEffectSetting>
                    {
                        new DeviceEffectSetting { Name = "Time", Value = 0, DefaultValue = 0 }
                    }
                }
            }
        };

        dataServiceMock.Setup(m => m.GetDeviceByNameAsync("VentrisDualReverb")).ReturnsAsync(device);
        dataServiceMock.Setup(m => m.GetControlChangeMessageToSetDeviceEffectSettingAsync("VentrisDualReverb", "ReverbEngineA", "Time", 64))
            .ReturnsAsync(new ControlChangeMessage { Address = 10, Value = 64 });
        
        var handler = new SetEffectHandler(loggerMock.Object, dataServiceMock.Object, deviceServiceMock.Object);
        var request = new SetEffectRequest("VentrisDualReverb", "ReverbEngineA", "Time", Value: 64);

        // Act
        var result = await handler.HandleAsync(request);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(200, status);
        Assert.Contains("Effect set for device VentrisDualReverb to 64", body);
        deviceServiceMock.Verify(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync("Conn", It.Is<ControlChangeMessage>(msg => msg.Address == 10 && msg.Value == 64)), Times.Once);
        dataServiceMock.Verify(m => m.UpdateDeviceByNameAsync("VentrisDualReverb", It.Is<MidiDevice>(d => d.DeviceEffects[0].EffectSettings[0].Value == 64)), Times.Once);
    }

    [Fact]
    public async Task SetEffect_ConfigurationNotFound_Returns404()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SetEffectHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();
        var deviceServiceMock = new Mock<IMidiDeviceService>();

        var device = new MidiDevice
        {
            Name = "VentrisDualReverb",
            Description = "Desc",
            MidiConnectName = "Conn",
            MidiImplementation = new List<MidiConfiguration>(), // Empty implementation
            DeviceEffects = new List<DeviceEffect>
            {
                new DeviceEffect
                {
                    Name = "ReverbEngineA",
                    Active = true,
                    EffectSettings = new List<DeviceEffectSetting>
                    {
                        new DeviceEffectSetting { Name = "Time", Value = 0, DefaultValue = 0 }
                    }
                }
            }
        };

        dataServiceMock.Setup(m => m.GetDeviceByNameAsync("VentrisDualReverb")).ReturnsAsync(device);
        dataServiceMock.Setup(m => m.GetControlChangeMessageToSetDeviceEffectSettingAsync("VentrisDualReverb", "ReverbEngineA", "Time", 64))
            .ThrowsAsync(new InvalidOperationException("Midi configuration 'Time' not found"));
        
        var handler = new SetEffectHandler(loggerMock.Object, dataServiceMock.Object, deviceServiceMock.Object);
        var request = new SetEffectRequest("VentrisDualReverb", "ReverbEngineA", "Time", Value: 64);

        // Act
        var result = await handler.HandleAsync(request);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(404, status);
        Assert.Contains("Midi configuration \\u0027Time\\u0027 not found", body);
    }

    [Fact]
    public async Task SetEffect_CcAddressNotFound_Returns404()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SetEffectHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();
        var deviceServiceMock = new Mock<IMidiDeviceService>();

        var device = new MidiDevice
        {
            Name = "VentrisDualReverb",
            Description = "Desc",
            MidiConnectName = "Conn",
            MidiImplementation = new List<MidiConfiguration>
            {
                new MidiConfiguration
                {
                    Name = "Time",
                    ControlChangeAddresses = new List<ControlChangeAddress>() // Empty addresses
                }
            },
            DeviceEffects = new List<DeviceEffect>
            {
                new DeviceEffect
                {
                    Name = "ReverbEngineA",
                    Active = true,
                    EffectSettings = new List<DeviceEffectSetting>
                    {
                        new DeviceEffectSetting { Name = "Time", Value = 0, DefaultValue = 0 }
                    }
                }
            }
        };

        dataServiceMock.Setup(m => m.GetDeviceByNameAsync("VentrisDualReverb")).ReturnsAsync(device);
        dataServiceMock.Setup(m => m.GetControlChangeMessageToSetDeviceEffectSettingAsync("VentrisDualReverb", "ReverbEngineA", "Time", 64))
            .ThrowsAsync(new InvalidOperationException("No control change addresses defined"));
        
        var handler = new SetEffectHandler(loggerMock.Object, dataServiceMock.Object, deviceServiceMock.Object);
        var request = new SetEffectRequest("VentrisDualReverb", "ReverbEngineA", "Time", Value: 64);

        // Act
        var result = await handler.HandleAsync(request);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(404, status);
        Assert.Contains("No control change addresses defined", body);
    }

    [Fact]
    public async Task SetEffect_ExceptionThrown_Returns500()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SetEffectHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();
        var deviceServiceMock = new Mock<IMidiDeviceService>();
        
        dataServiceMock.Setup(m => m.GetDeviceByNameAsync(It.IsAny<string>())).ThrowsAsync(new Exception("DB Error"));
        
        var handler = new SetEffectHandler(loggerMock.Object, dataServiceMock.Object, deviceServiceMock.Object);
        var request = new SetEffectRequest("Device", "Effect", "Setting");

        // Act
        var result = await handler.HandleAsync(request);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(500, status);
        Assert.Contains("DB Error", body);
    }

    [Fact]
    public async Task SetEffect_ValidSelectionRequest_ReturnsOk()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SetEffectHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();
        var deviceServiceMock = new Mock<IMidiDeviceService>();

        var device = new MidiDevice
        {
            Name = "VentrisDualReverb",
            Description = "Desc",
            MidiConnectName = "Conn",
            MidiImplementation = new List<MidiConfiguration>
            {
                new MidiConfiguration
                {
                    Name = "ReverbEngine",
                    ControlChangeAddresses = new List<ControlChangeAddress>
                    {
                        new ControlChangeAddress { Name = "ReverbEngineA", Value = 1 }
                    }
                }
            },
            DeviceEffects = new List<DeviceEffect>
            {
                new DeviceEffect
                {
                    Name = "ReverbEngineA",
                    Active = true,
                    EffectSettings = new List<DeviceEffectSetting>
                    {
                        new DeviceEffectSetting { Name = "ReverbEngine", Value = 0, DefaultValue = 0 }
                    }
                }
            }
        };

        var selector = new Selector
        {
            Name = "ReverbEngine",
            Selections = new List<MidiSelection>
            {
                new MidiSelection { Name = "Room", ControlChangeMessageValue = 0 }
            }
        };

        dataServiceMock.Setup(m => m.GetDeviceByNameAsync("VentrisDualReverb")).ReturnsAsync(device);
        dataServiceMock.Setup(m => m.GetSelectorByNameAsync("ReverbEngine")).ReturnsAsync(selector);
        dataServiceMock.Setup(m => m.GetControlChangeMessageToSetDeviceEffectSettingSelectionAsync("VentrisDualReverb", "ReverbEngineA", "ReverbEngine", "Room"))
            .ReturnsAsync(new ControlChangeMessage { Address = 1, Value = 0 });
        
        var handler = new SetEffectHandler(loggerMock.Object, dataServiceMock.Object, deviceServiceMock.Object);
        var request = new SetEffectRequest("VentrisDualReverb", "ReverbEngineA", "ReverbEngine", Selection: "Room");

        // Act
        var result = await handler.HandleAsync(request);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(200, status);
        Assert.Contains("Effect set for device VentrisDualReverb to 0", body);
        deviceServiceMock.Verify(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync("Conn", It.Is<ControlChangeMessage>(msg => msg.Address == 1 && msg.Value == 0)), Times.Once);
        dataServiceMock.Verify(m => m.UpdateDeviceByNameAsync("VentrisDualReverb", It.Is<MidiDevice>(d => d.DeviceEffects[0].EffectSettings[0].Value == 0)), Times.Once);
    }

    [Fact]
    public async Task SetEffect_WithDependency_ReturnsOk()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SetEffectHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();
        var deviceServiceMock = new Mock<IMidiDeviceService>();

        var deviceName = "VentrisDualReverb";
        var effectName = "ReverbEngineA";
        var settingName = "Control1";
        var dependencyName = "ReverbEngine";
        var dependentEffectName = "Room";
        var implementationName = "EngineParameter1";

        var device = new MidiDevice
        {
            Name = deviceName,
            Description = "Desc",
            MidiConnectName = "Conn",
            MidiImplementation = new List<MidiConfiguration>
            {
                new MidiConfiguration
                {
                    Name = implementationName,
                    ControlChangeAddresses = new List<ControlChangeAddress>
                    {
                        new ControlChangeAddress { Name = effectName, Value = 15 }
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
                            Name = settingName, 
                            Value = 0, 
                            DefaultValue = 0,
                            DeviceEffectSettingDependencyName = dependencyName 
                        },
                        new DeviceEffectSetting
                        {
                            Name = dependencyName,
                            Value = 10,
                            DefaultValue = 0
                        }
                    }
                }
            }
        };

        var selector = new Selector
        {
            Name = dependencyName,
            Selections = new List<MidiSelection>
            {
                new MidiSelection { Name = dependentEffectName, ControlChangeMessageValue = 10 }
            }
        };

        var dependentEffect = new Effect
        {
            Name = dependentEffectName,
            Description = "Desc",
            DeviceSettings = new List<DeviceSetting>
            {
                new DeviceSetting
                {
                    Name = settingName,
                    DeviceName = deviceName,
                    EffectName = dependentEffectName,
                    DeviceMidiImplementationName = implementationName
                }
            }
        };

        dataServiceMock.Setup(m => m.GetDeviceByNameAsync(deviceName)).ReturnsAsync(device);
        dataServiceMock.Setup(m => m.GetSelectorByNameAsync(dependencyName)).ReturnsAsync(selector);
        dataServiceMock.Setup(m => m.GetEffectByNameAsync(dependentEffectName)).ReturnsAsync(dependentEffect);
        dataServiceMock.Setup(m => m.GetControlChangeMessageToSetDeviceEffectSettingAsync(deviceName, effectName, settingName, 127))
            .ReturnsAsync(new ControlChangeMessage { Address = 15, Value = 127 });
        
        var handler = new SetEffectHandler(loggerMock.Object, dataServiceMock.Object, deviceServiceMock.Object);
        var request = new SetEffectRequest(deviceName, effectName, settingName, Value: 127);

        // Act
        var result = await handler.HandleAsync(request);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(200, status);
        Assert.Contains($"Effect set for device {deviceName} to 127", body);
        deviceServiceMock.Verify(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync("Conn", It.Is<ControlChangeMessage>(msg => msg.Address == 15 && msg.Value == 127)), Times.Once);
        dataServiceMock.Verify(m => m.UpdateDeviceByNameAsync(deviceName, It.IsAny<MidiDevice>()), Times.Once);
    }
}
