using System.Reflection;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.UnitTests;

public class MidiEndpointsTests
{
    private const string TestDeviceName = "TestDevice";
    
    private const string TestEffectName = "ReverbEngine";

    private static readonly MidiDevice TestDevice = new()
    {
        Name = TestDeviceName,
        Description = "desc",
        Active = true,
        MidiImplementation = new List<MidiConfiguration>(),
        DeviceEffects = new List<DeviceEffect> { new() 
            {
                Name = TestEffectName,
                Active = false,
                DefaultActive = true,
                DeviceEffectSettings = new List<DeviceEffectSetting>
                {
                    new() { Name = "SettingA", Value = 3 }
                }
            }
        }
    };

    private static MethodInfo GetResetDeviceMethod()
    {
        var mi = typeof(MinimalApi.Endpoints.MidiEndpoints)
            .GetMethod("ResetDevice", BindingFlags.NonPublic | BindingFlags.Static);
        return mi ?? throw new InvalidOperationException("ResetDevice method not found");
    }

    private static async Task<IResult> InvokeResetDeviceAsync(ILogger logger, IMidiDeviceService deviceService, IMidiDataService dataService)
    {
        var mi = GetResetDeviceMethod();
        var task = (Task)mi.Invoke(null, new object[] { logger, deviceService, dataService, TestDeviceName })!;
        await task.ConfigureAwait(false);
        var result = task.GetType().GetProperty("Result")!.GetValue(task) as IResult;
        return result!;
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
        var loggerMock = new Mock<ILogger>();
        var deviceServiceMock = new Mock<IMidiDeviceService>();
        var dataServiceMock = new Mock<IMidiDataService>();

        dataServiceMock.Setup(m => m.GetDeviceByNameAsync(TestDeviceName)).ReturnsAsync(TestDevice);

        var activateMsg = new ControlChangeMessage { Address = 1, Value = 1 };
        var revertMsg = new ControlChangeMessage { Address = 1, Value = 0 };

        dataServiceMock.Setup(m => m.GetControlChangeMessageToActivateDeviceEffectAsync(TestDeviceName, TestEffectName, true)).ReturnsAsync(activateMsg);
        dataServiceMock.Setup(m => m.GetControlChangeMessageToActivateDeviceEffectAsync(TestDeviceName, TestEffectName, false)).ReturnsAsync(revertMsg);

        deviceServiceMock.Setup(m => m.SendControlChangeMessageByDeviceNameAsync(TestDeviceName, activateMsg)).ReturnsAsync(activateMsg);
        deviceServiceMock.Setup(m => m.SendControlChangeMessageByDeviceNameAsync(TestDeviceName, revertMsg)).ReturnsAsync(revertMsg);

        dataServiceMock.Setup(m => m.UpdateDeviceEffectActiveStateAsync(TestDeviceName, TestEffectName, true)).Returns(Task.CompletedTask);

        dataServiceMock.Setup(m => m.GetControlChangeMessageToSetDeviceEffectSettingAsync(TestDeviceName, TestEffectName, "SettingA", 3))
            .ReturnsAsync(new ControlChangeMessage { Address = 2, Value = 3 });

        // Act
        var result = await InvokeResetDeviceAsync(loggerMock.Object, deviceServiceMock.Object, dataServiceMock.Object);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(200, status);
        Assert.Contains("reset to default settings successfully.", body);

        deviceServiceMock.Verify(m => m.SendControlChangeMessageByDeviceNameAsync(TestDeviceName, It.IsAny<ControlChangeMessage>()), Times.Once);
        dataServiceMock.Verify(m => m.UpdateDeviceEffectActiveStateAsync(TestDeviceName, TestEffectName, true), Times.Once);
    }

    [Fact]
    public async Task ResetDevice_UpdateFails_RevertSucceeds_Returns500()
    {
        // Arrange
        var loggerMock = new Mock<ILogger>();
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
            .Setup(m => m.SendControlChangeMessageByDeviceNameAsync(It.IsAny<string>(), It.IsAny<ControlChangeMessage>()))
            .Callback<string, ControlChangeMessage>((_, msg) => capturedSent = msg)
            .ReturnsAsync((string _, ControlChangeMessage m) => m);

        // Simulate DB update failure so revert path is executed
        dataServiceMock.Setup(m => m.UpdateDeviceEffectActiveStateAsync(TestDeviceName, TestEffectName, true)).ThrowsAsync(new Exception("DB failure"));

        // Act
        var result = await InvokeResetDeviceAsync(loggerMock.Object, deviceServiceMock.Object, dataServiceMock.Object);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(500, status);
        Assert.Contains("Device reset completed with errors", body);

        // Revert should have been attempted (device service called at least once for revert)
        deviceServiceMock.Verify(m => m.SendControlChangeMessageByDeviceNameAsync(TestDeviceName, It.IsAny<ControlChangeMessage>()), Times.AtLeastOnce);
        Assert.NotNull(capturedSent);
        dataServiceMock.Verify(m => m.UpdateDeviceEffectActiveStateAsync(TestDeviceName, TestEffectName, true), Times.Once);
    }

    [Fact]
    public async Task ResetDevice_UpdateFails_RevertFails_Returns500WithDetail()
    {
        // Arrange
        var loggerMock = new Mock<ILogger>();
        var deviceServiceMock = new Mock<IMidiDeviceService>();
        var dataServiceMock = new Mock<IMidiDataService>();

        var device = new MidiDevice
        {
            Name = TestDeviceName,
            Description = "desc",
            Active = true,
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
        deviceServiceMock.Setup(m => m.SendControlChangeMessageByDeviceNameAsync(TestDeviceName, It.Is<ControlChangeMessage>(c => c.Value == activateMsg.Value))).ReturnsAsync(activateMsg);
        deviceServiceMock.Setup(m => m.SendControlChangeMessageByDeviceNameAsync(TestDeviceName, It.Is<ControlChangeMessage>(c => c.Value == revertMsg.Value))).ThrowsAsync(new Exception("Device revert failed"));

        // Simulate DB update failure so revert path is executed
        dataServiceMock.Setup(m => m.UpdateDeviceEffectActiveStateAsync(TestDeviceName, TestEffectName, true)).ThrowsAsync(new Exception("DB failure"));

        // Act
        var result = await InvokeResetDeviceAsync(loggerMock.Object, deviceServiceMock.Object, dataServiceMock.Object);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(500, status);
        Assert.Contains("Failed to revert effect", body);

        deviceServiceMock.Verify(m => m.SendControlChangeMessageByDeviceNameAsync(TestDeviceName, It.IsAny<ControlChangeMessage>()), Times.AtLeastOnce);
        dataServiceMock.Verify(m => m.UpdateDeviceEffectActiveStateAsync(TestDeviceName, TestEffectName, true), Times.Once);
    }
}
