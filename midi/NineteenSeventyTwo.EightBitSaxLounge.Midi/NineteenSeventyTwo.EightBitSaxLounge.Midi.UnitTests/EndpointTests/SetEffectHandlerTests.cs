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
        
        var handler = new SetEffectHandler(loggerMock.Object, dataServiceMock.Object, deviceServiceMock.Object);
        var request = new SetEffectRequest("VentrisDualReverb", "ReverbEngineA", "Time", Value: 64);

        // Act
        var result = await handler.HandleAsync(request);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(404, status);
        Assert.Contains("Control change address for effect \\u0027ReverbEngineA\\u0027 not found", body);
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
}
