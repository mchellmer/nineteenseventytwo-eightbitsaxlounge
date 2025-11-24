using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Handlers;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.UnitTests;

public class MidiEndpointsTests
{
    private const string TestDeviceName = "TestDevice";
    private const string TestMidiConnectName = "TestMidiConnect"; // added constant
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
                DeviceEffectSettings = new List<DeviceEffectSetting>
                {
                    new() {
                        Name = "SettingA",
                        DefaultValue = 0,
                        Value = 3 }
                }
            }
        }
    };

    private static Task<IResult> InvokeResetDeviceAsync(ILogger<MidiEndpointsHandler> logger, IMidiDeviceService deviceService, IMidiDataService dataService)
    {
        // construct handler directly with DI-style constructor parameters (mocks in tests)
        var handler = new MidiEndpointsHandler(logger, deviceService, dataService);
        return handler.ResetDevice(TestDeviceName);
    }

    private static Task<IResult> InvokeSendCcAsync(ILogger<MidiEndpointsHandler> logger, IMidiDeviceService deviceService, IMidiDataService dataService, string midiConnectName, int address, int value)
    {
        var handler = new MidiEndpointsHandler(logger, deviceService, dataService);
        return handler.PostControlChangeMessageToDeviceByMidiConnectName(midiConnectName, address, value);
    }

    private static async Task<(int StatusCode, string Body)> ExecuteResultAsync(IResult result)
    {
        // Try to extract common properties without executing the result (avoids needing RequestServices)
        var type = result.GetType();

        // Status code property (common on many Result types)
        var statusProp = type.GetProperty("StatusCode");
        int status = statusProp != null && statusProp.GetValue(result) is int s ? s : 200;

        // Common value/payload property names used by different IResult implementations
        var valueProp = type.GetProperty("Value")
                     ?? type.GetProperty("Payload")
                     ?? type.GetProperty("Result")
                     ?? type.GetProperty("ValueAsObject")
                     ?? type.GetProperty("Content");

        if (valueProp != null)
        {
            var value = valueProp.GetValue(result);
            if (value == null)
                return (status, string.Empty);

            if (value is string str)
                return (status, str);

            try
            {
                var body = JsonSerializer.Serialize(value);
                return (status, body);
            }
            catch
            {
                return (status, value.ToString() ?? string.Empty);
            }
        }

        // Fallback: execute the result but ensure RequestServices is not null to prevent ArgumentNullException
        var ctx = new DefaultHttpContext();
        ctx.RequestServices = new ServiceCollection()
            .AddLogging() // provide minimal services commonly required by result execution
            .BuildServiceProvider();
        ctx.Response.Body = new MemoryStream();

        await result.ExecuteAsync(ctx).ConfigureAwait(false);

        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        using var sr = new StreamReader(ctx.Response.Body);
        var bodyText = await sr.ReadToEndAsync().ConfigureAwait(false);
        return (ctx.Response.StatusCode, bodyText);
    }

    [Fact]
    public async Task ResetDevice_Success_Returns200()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MidiEndpointsHandler>>();
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

        dataServiceMock.Setup(m => m.GetControlChangeMessageToSetDeviceEffectSettingAsync(TestDeviceName, TestEffectName, "SettingA", 3))
            .ReturnsAsync(new ControlChangeMessage { Address = 2, Value = 3 });

        // Act
        var result = await InvokeResetDeviceAsync(loggerMock.Object, deviceServiceMock.Object, dataServiceMock.Object);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(200, status);
        Assert.Contains("reset to default settings successfully.", body);

        deviceServiceMock.Verify(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync(TestMidiConnectName, It.IsAny<ControlChangeMessage>()), Times.Once);
        dataServiceMock.Verify(m => m.UpdateDeviceEffectActiveStateAsync(TestDeviceName, TestEffectName, true), Times.Once);
    }

    [Fact]
    public async Task ResetDevice_UpdateFails_RevertSucceeds_Returns500()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MidiEndpointsHandler>>();
        var deviceServiceMock = new Mock<IMidiDeviceService>();
        var dataServiceMock = new Mock<IMidiDataService>();

        dataServiceMock.Setup(m => m.GetDeviceByNameAsync(TestDeviceName)).ReturnsAsync(TestDevice);

        var activateMsg = new ControlChangeMessage { Address = 1, Value = 1 };
        var revertMsg = new ControlChangeMessage { Address = 1, Value = 0 };

        dataServiceMock.Setup(m => m.GetControlChangeMessageToActivateDeviceEffectAsync(TestDeviceName, TestEffectName, true)).ReturnsAsync(activateMsg);
        dataServiceMock.Setup(m => m.GetControlChangeMessageToActivateDeviceEffectAsync(TestDeviceName, TestEffectName, false)).ReturnsAsync(revertMsg);

        // Capture sent messages to assert later
        ControlChangeMessage? capturedSent = null;
        deviceServiceMock
            .Setup(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync(It.Is<string>(s => s == TestMidiConnectName), It.IsAny<ControlChangeMessage>()))
            .Callback<string, ControlChangeMessage>((_, msg) => capturedSent = msg)
            .Returns((string _, ControlChangeMessage m) => Task.FromResult(m));

        // Simulate DB update failure so revert path is executed
        dataServiceMock.Setup(m => m.UpdateDeviceEffectActiveStateAsync(TestDeviceName, TestEffectName, true)).ThrowsAsync(new Exception("DB failure"));

        // Act
        var result = await InvokeResetDeviceAsync(loggerMock.Object, deviceServiceMock.Object, dataServiceMock.Object);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(500, status);
        Assert.Contains("Device reset completed with errors", body);

        deviceServiceMock.Verify(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync(TestMidiConnectName, It.IsAny<ControlChangeMessage>()), Times.AtLeastOnce);
        Assert.NotNull(capturedSent);
        dataServiceMock.Verify(m => m.UpdateDeviceEffectActiveStateAsync(TestDeviceName, TestEffectName, true), Times.Once);
    }

    [Fact]
    public async Task ResetDevice_UpdateFails_RevertFails_Returns500WithDetail()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MidiEndpointsHandler>>();
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
                    DeviceEffectSettings = new List<DeviceEffectSetting>()
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

        // Act
        var result = await InvokeResetDeviceAsync(loggerMock.Object, deviceServiceMock.Object, dataServiceMock.Object);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(500, status);
        Assert.Contains("Failed to revert effect", body);

        deviceServiceMock.Verify(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync(TestMidiConnectName, It.IsAny<ControlChangeMessage>()), Times.AtLeastOnce);
        dataServiceMock.Verify(m => m.UpdateDeviceEffectActiveStateAsync(TestDeviceName, TestEffectName, true), Times.Once);
    }

    [Fact]
    public async Task SendControlChangeMessage_Success_Returns200()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MidiEndpointsHandler>>();
        var deviceServiceMock = new Mock<IMidiDeviceService>();
        var dataServiceMock = new Mock<IMidiDataService>();

        ControlChangeMessage? sentMessage = null;
        deviceServiceMock.Setup(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync(TestMidiConnectName, It.IsAny<ControlChangeMessage>()))
            .Callback<string, ControlChangeMessage>((_, msg) => sentMessage = msg)
            .Returns((string _, ControlChangeMessage m) => Task.FromResult(m));

        int address = 10;
        int value = 64;
        // Act
        var result = await InvokeSendCcAsync(loggerMock.Object, deviceServiceMock.Object, dataServiceMock.Object, TestMidiConnectName, address, value);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(200, status);
        Assert.Contains("processed successfully", body);
        Assert.NotNull(sentMessage);
        Assert.Equal(address, sentMessage!.Address);
        Assert.Equal(value, sentMessage.Value);
        deviceServiceMock.Verify(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync(TestMidiConnectName, It.IsAny<ControlChangeMessage>()), Times.Once);
    }

    [Fact]
    public async Task SendControlChangeMessage_Failure_Returns500()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MidiEndpointsHandler>>();
        var deviceServiceMock = new Mock<IMidiDeviceService>();
        var dataServiceMock = new Mock<IMidiDataService>();

        deviceServiceMock.Setup(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync(TestMidiConnectName, It.IsAny<ControlChangeMessage>()))
            .ThrowsAsync(new Exception("Native send failure"));

        // Act
        var result = await InvokeSendCcAsync(loggerMock.Object, deviceServiceMock.Object, dataServiceMock.Object, TestMidiConnectName, 7, 127);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(500, status);
        Assert.Contains("Failed to send Control Change Message", body);
        deviceServiceMock.Verify(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync(TestMidiConnectName, It.IsAny<ControlChangeMessage>()), Times.Once);
    }
}
