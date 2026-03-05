using Microsoft.Extensions.Logging;
using Moq;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Handlers;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.UnitTests.EndpointTests;

public class ResetDeviceHandlerTests : TestBase
{
    private const string TestDeviceName = "TestDevice";
    private const string TestMidiConnectName = "TestMidiConnect";
    private const string TestEffectName = "ReverbEngine";

    private static readonly MidiDevice TestDevice = new()
    {
        Name = TestDeviceName,
        Description = "desc",
        Active = true,
        MidiImplementation = new List<MidiConfiguration>(),
        MidiConnectName = TestMidiConnectName,
        DeviceEffects = new List<DeviceEffect> { new()
            {
                Name = TestEffectName,
                Active = false,
                DefaultActive = true,
                EffectSettings = new List<DeviceEffectSetting>
                {
                    new() {
                        Name = "SettingA",
                        DefaultValue = 0,
                        Value = 3 }
                }
            }
        }
    };

    [Fact]
    public async Task ResetDevice_Success_Returns200()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ResetDeviceHandler>>();
        var deviceServiceMock = new Mock<IMidiDeviceService>();
        var dataServiceMock = new Mock<IMidiDataService>();

        dataServiceMock.Setup(m => m.GetDeviceByNameAsync(TestDeviceName)).ReturnsAsync(TestDevice);

        var activateMsg = new ControlChangeMessage { Address = 1, Value = 1 };
        var revertMsg = new ControlChangeMessage { Address = 1, Value = 0 };

        dataServiceMock.Setup(m => m.GetControlChangeMessageToActivateDeviceEffectAsync(TestDeviceName, TestEffectName, true)).ReturnsAsync(activateMsg);
        dataServiceMock.Setup(m => m.GetControlChangeMessageToActivateDeviceEffectAsync(TestDeviceName, TestEffectName, false)).ReturnsAsync(revertMsg);

        // Expect call using MidiConnectName
        deviceServiceMock.Setup(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync(TestMidiConnectName, activateMsg)).Returns(Task.FromResult(activateMsg));

        dataServiceMock.Setup(m => m.UpdateDeviceEffectActiveStateAsync(TestDeviceName, TestEffectName, true)).Returns(Task.CompletedTask);

        var settingMsg = new ControlChangeMessage { Address = 2, Value = 0 };
        dataServiceMock.Setup(m => m.GetControlChangeMessageToSetDeviceEffectSettingAsync(TestDeviceName, TestEffectName, "SettingA", 0))
            .ReturnsAsync(settingMsg);

        deviceServiceMock.Setup(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync(TestMidiConnectName, settingMsg)).Returns(Task.FromResult(settingMsg));
        dataServiceMock.Setup(m => m.UpdateDeviceByNameAsync(TestDeviceName, TestDevice)).Returns(Task.CompletedTask);

        var handler = new ResetDeviceHandler(loggerMock.Object, deviceServiceMock.Object, dataServiceMock.Object);

        // Act
        var result = await handler.HandleAsync(TestDeviceName);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(200, status);
        Assert.Contains("reset to default settings successfully.", body);

        deviceServiceMock.Verify(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync(TestMidiConnectName, activateMsg), Times.Once);
        deviceServiceMock.Verify(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync(TestMidiConnectName, settingMsg), Times.Once);
        dataServiceMock.Verify(m => m.UpdateDeviceEffectActiveStateAsync(TestDeviceName, TestEffectName, true), Times.Once);
        dataServiceMock.Verify(m => m.UpdateDeviceByNameAsync(TestDeviceName, TestDevice), Times.Once);
    }

    [Fact]
    public async Task ResetDevice_UpdateFails_RevertSucceeds_Returns500()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ResetDeviceHandler>>();
        var deviceServiceMock = new Mock<IMidiDeviceService>();
        var dataServiceMock = new Mock<IMidiDataService>();

        dataServiceMock.Setup(m => m.GetDeviceByNameAsync(TestDeviceName)).ReturnsAsync(TestDevice);

        var activateMsg = new ControlChangeMessage { Address = 1, Value = 1 };
        var revertMsg = new ControlChangeMessage { Address = 1, Value = 0 };

        dataServiceMock.Setup(m => m.GetControlChangeMessageToActivateDeviceEffectAsync(TestDeviceName, TestEffectName, true)).ReturnsAsync(activateMsg);
        dataServiceMock.Setup(m => m.GetControlChangeMessageToActivateDeviceEffectAsync(TestDeviceName, TestEffectName, false)).ReturnsAsync(revertMsg);

        // Capture sent messages to assert later
        var capturedMessages = new List<ControlChangeMessage>();
        deviceServiceMock
            .Setup(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync(It.Is<string>(s => s == TestMidiConnectName), It.IsAny<ControlChangeMessage>()))
            .Callback<string, ControlChangeMessage>((_, msg) => capturedMessages.Add(msg))
            .Returns((string _, ControlChangeMessage m) => Task.FromResult(m));

        // Simulate DB update failure so revert path is executed
        dataServiceMock.Setup(m => m.UpdateDeviceEffectActiveStateAsync(TestDeviceName, TestEffectName, true)).ThrowsAsync(new Exception("DB failure"));

        var handler = new ResetDeviceHandler(loggerMock.Object, deviceServiceMock.Object, dataServiceMock.Object);

        // Act
        var result = await handler.HandleAsync(TestDeviceName);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(500, status);
        Assert.Contains("Device reset completed with errors", body);

        deviceServiceMock.Verify(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync(TestMidiConnectName, It.IsAny<ControlChangeMessage>()), Times.AtLeastOnce);
        Assert.NotEmpty(capturedMessages);
        dataServiceMock.Verify(m => m.UpdateDeviceEffectActiveStateAsync(TestDeviceName, TestEffectName, true), Times.Once);
    }

    [Fact]
    public async Task ResetDevice_UpdateFails_RevertFails_Returns500WithDetail()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<ResetDeviceHandler>>();
        var deviceServiceMock = new Mock<IMidiDeviceService>();
        var dataServiceMock = new Mock<IMidiDataService>();

        var device = new MidiDevice
        {
            Name = TestDeviceName,
            Description = "desc",
            Active = true,
            MidiConnectName = TestMidiConnectName,
            MidiImplementation = new List<MidiConfiguration>(),
            DeviceEffects = new List<DeviceEffect>
            {
                new()
                {
                    Name = TestEffectName,
                    Active = false,
                    DefaultActive = true,
                    EffectSettings = new List<DeviceEffectSetting>()
                }
            }
        };

        dataServiceMock.Setup(m => m.GetDeviceByNameAsync(TestDeviceName)).ReturnsAsync(device);

        var activateMsg = new ControlChangeMessage { Address = 1, Value = 1 };
        var revertMsg = new ControlChangeMessage { Address = 1, Value = 0 };

        dataServiceMock.Setup(m => m.GetControlChangeMessageToActivateDeviceEffectAsync(TestDeviceName, TestEffectName, true)).ReturnsAsync(activateMsg);
        dataServiceMock.Setup(m => m.GetControlChangeMessageToActivateDeviceEffectAsync(TestDeviceName, TestEffectName, false)).ReturnsAsync(revertMsg);

        // Simulate sending activate succeeds but sending revert throws
        deviceServiceMock.Setup(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync(TestMidiConnectName, It.Is<ControlChangeMessage>(c => c.Value == activateMsg.Value))).Returns(Task.FromResult(activateMsg));
        deviceServiceMock.Setup(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync(TestMidiConnectName, It.Is<ControlChangeMessage>(c => c.Value == revertMsg.Value))).ThrowsAsync(new Exception("Device revert failed"));

        // Simulate DB update failure so revert path is executed
        dataServiceMock.Setup(m => m.UpdateDeviceEffectActiveStateAsync(TestDeviceName, TestEffectName, true)).ThrowsAsync(new Exception("DB failure"));

        var handler = new ResetDeviceHandler(loggerMock.Object, deviceServiceMock.Object, dataServiceMock.Object);

        // Act
        var result = await handler.HandleAsync(TestDeviceName);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(500, status);
        Assert.Contains("Failed to revert effect", body);

        deviceServiceMock.Verify(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync(TestMidiConnectName, It.IsAny<ControlChangeMessage>()), Times.AtLeastOnce);
        dataServiceMock.Verify(m => m.UpdateDeviceEffectActiveStateAsync(TestDeviceName, TestEffectName, true), Times.Once);
    }
}
