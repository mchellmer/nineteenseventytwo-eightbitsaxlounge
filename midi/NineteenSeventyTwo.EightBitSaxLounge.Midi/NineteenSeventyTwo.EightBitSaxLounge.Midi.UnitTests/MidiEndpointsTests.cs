using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Handlers;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Models;

using System.Text.Json;

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

    private static Task<IResult> InvokeResetDeviceAsync(ILogger<MidiEndpointsHandler> logger, IMidiDeviceService deviceService, IMidiDataService dataService, IOptions<DatabaseOptions>? databaseOptions = null)
    {
        var dbOpts = databaseOptions ?? Options.Create(new DatabaseOptions { Names = ["devices"] });
        var effectsOpts = Options.Create(new EffectsOptions());
        var handler = new MidiEndpointsHandler(logger, deviceService, dataService, dbOpts, effectsOpts);
        return handler.ResetDevice(TestDeviceName);
    }

    private static Task<IResult> InvokeSendCcAsync(ILogger<MidiEndpointsHandler> logger, IMidiDeviceService deviceService, IMidiDataService dataService, string midiConnectName, int address, int value, IOptions<DatabaseOptions>? databaseOptions = null)
    {
        var dbOpts = databaseOptions ?? Options.Create(new DatabaseOptions { Names = ["devices"] });
        var effectsOpts = Options.Create(new EffectsOptions());
        var handler = new MidiEndpointsHandler(logger, deviceService, dataService, dbOpts, effectsOpts);
        return handler.PostControlChangeMessageToDeviceByMidiConnectName(midiConnectName, address, value);
    }

    private static Task<IResult> InvokeInitializeDataModelAsync(ILogger<MidiEndpointsHandler> logger, IMidiDataService dataService, IOptions<DatabaseOptions> databaseOptions)
    {
        var deviceServiceMock = new Mock<IMidiDeviceService>();
        var effectsOpts = Options.Create(new EffectsOptions());
        var handler = new MidiEndpointsHandler(logger, deviceServiceMock.Object, dataService, databaseOptions, effectsOpts);
        return handler.InitializeDataModel();
    }

    private static Task<IResult> InvokeUploadEffectsAsync(ILogger<MidiEndpointsHandler> logger, IMidiDataService dataService, IOptions<EffectsOptions> effectsOptions, IOptions<DatabaseOptions>? databaseOptions = null)
    {
        var deviceServiceMock = new Mock<IMidiDeviceService>();
        var dbOpts = databaseOptions ?? Options.Create(new DatabaseOptions { Names = ["effects"] });
        var handler = new MidiEndpointsHandler(logger, deviceServiceMock.Object, dataService, dbOpts, effectsOptions);
        return handler.UploadEffects();
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

    [Fact]
    public async Task InitializeDataModel_Success_Returns200()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MidiEndpointsHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();

        var dbNames = new List<string> { "devices", "effects", "selectors" };
        var databaseOptions = Options.Create(new DatabaseOptions { Names = dbNames });

        dataServiceMock.Setup(m => m.CreateDatabaseAsync("devices")).Returns(Task.CompletedTask);
        dataServiceMock.Setup(m => m.CreateDatabaseAsync("effects")).Returns(Task.CompletedTask);
        dataServiceMock.Setup(m => m.CreateDatabaseAsync("selectors")).Returns(Task.CompletedTask);

        // Act
        var result = await InvokeInitializeDataModelAsync(loggerMock.Object, dataServiceMock.Object, databaseOptions);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(200, status);
        Assert.Contains("MIDI data model initialized successfully", body);
        Assert.Contains("devices", body);
        Assert.Contains("effects", body);
        Assert.Contains("selectors", body);

        dataServiceMock.Verify(m => m.CreateDatabaseAsync("devices"), Times.Once);
        dataServiceMock.Verify(m => m.CreateDatabaseAsync("effects"), Times.Once);
        dataServiceMock.Verify(m => m.CreateDatabaseAsync("selectors"), Times.Once);
    }

    [Fact]
    public async Task InitializeDataModel_DatabaseCreationFails_Returns500()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MidiEndpointsHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();

        var dbNames = new List<string> { "devices", "effects", "selectors" };
        var databaseOptions = Options.Create(new DatabaseOptions { Names = dbNames });

        dataServiceMock.Setup(m => m.CreateDatabaseAsync("devices"))
            .ThrowsAsync(new Exception("Database creation failed"));

        // Act
        var result = await InvokeInitializeDataModelAsync(loggerMock.Object, dataServiceMock.Object, databaseOptions);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(500, status);
        Assert.Contains("Data model initialization failed", body);
        Assert.Contains("devices", body);

        dataServiceMock.Verify(m => m.CreateDatabaseAsync("devices"), Times.Once);
    }

    [Fact]
    public async Task UploadEffects_NoEffectsInConfig_Returns200()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MidiEndpointsHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();

        var effectsOptions = Options.Create(new EffectsOptions { Effects = new List<Effect>() });

        // Act
        var result = await InvokeUploadEffectsAsync(loggerMock.Object, dataServiceMock.Object, effectsOptions);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(200, status);
        Assert.Contains("No effects found in configuration", body);

        dataServiceMock.Verify(m => m.GetAllEffectsAsync(), Times.Never);
        dataServiceMock.Verify(m => m.CreateEffectAsync(It.IsAny<string>(), It.IsAny<Effect>()), Times.Never);
        dataServiceMock.Verify(m => m.UpdateEffectByNameAsync(It.IsAny<string>(), It.IsAny<Effect>()), Times.Never);
    }

    [Fact]
    public async Task UploadEffects_AllNewEffects_CreatesAll()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MidiEndpointsHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();

        var effects = new List<Effect>
        {
            new() { Name = "Reverb", Description = "Reverb effect" },
            new() { Name = "Delay", Description = "Delay effect" },
            new() { Name = "Chorus", Description = "Chorus effect" }
        };

        var effectsOptions = Options.Create(new EffectsOptions { Effects = effects });

        // Empty database - no existing effects
        dataServiceMock.Setup(m => m.GetAllEffectsAsync()).ReturnsAsync(new List<Effect>());

        dataServiceMock.Setup(m => m.CreateEffectAsync("Reverb", It.IsAny<Effect>())).Returns(Task.CompletedTask);
        dataServiceMock.Setup(m => m.CreateEffectAsync("Delay", It.IsAny<Effect>())).Returns(Task.CompletedTask);
        dataServiceMock.Setup(m => m.CreateEffectAsync("Chorus", It.IsAny<Effect>())).Returns(Task.CompletedTask);

        // Act
        var result = await InvokeUploadEffectsAsync(loggerMock.Object, dataServiceMock.Object, effectsOptions);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(200, status);
        Assert.Contains("Created: 3", body);
        Assert.Contains("Updated: 0", body);

        dataServiceMock.Verify(m => m.GetAllEffectsAsync(), Times.Once);
        dataServiceMock.Verify(m => m.CreateEffectAsync("Reverb", It.Is<Effect>(e => e.Name == "Reverb")), Times.Once);
        dataServiceMock.Verify(m => m.CreateEffectAsync("Delay", It.Is<Effect>(e => e.Name == "Delay")), Times.Once);
        dataServiceMock.Verify(m => m.CreateEffectAsync("Chorus", It.Is<Effect>(e => e.Name == "Chorus")), Times.Once);
        dataServiceMock.Verify(m => m.UpdateEffectByNameAsync(It.IsAny<string>(), It.IsAny<Effect>()), Times.Never);
    }

    [Fact]
    public async Task UploadEffects_AllExistingEffects_UpdatesAll()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MidiEndpointsHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();

        var effects = new List<Effect>
        {
            new() { Name = "Reverb", Description = "Updated reverb effect" },
            new() { Name = "Delay", Description = "Updated delay effect" }
        };

        var effectsOptions = Options.Create(new EffectsOptions { Effects = effects });

        // Existing effects in database
        var existingEffects = new List<Effect>
        {
            new() { Name = "Reverb", Description = "Old reverb effect" },
            new() { Name = "Delay", Description = "Old delay effect" }
        };

        dataServiceMock.Setup(m => m.GetAllEffectsAsync()).ReturnsAsync(existingEffects);

        dataServiceMock.Setup(m => m.UpdateEffectByNameAsync("Reverb", It.IsAny<Effect>())).Returns(Task.CompletedTask);
        dataServiceMock.Setup(m => m.UpdateEffectByNameAsync("Delay", It.IsAny<Effect>())).Returns(Task.CompletedTask);

        // Act
        var result = await InvokeUploadEffectsAsync(loggerMock.Object, dataServiceMock.Object, effectsOptions);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(200, status);
        Assert.Contains("Created: 0", body);
        Assert.Contains("Updated: 2", body);

        dataServiceMock.Verify(m => m.GetAllEffectsAsync(), Times.Once);
        dataServiceMock.Verify(m => m.UpdateEffectByNameAsync("Reverb", It.Is<Effect>(e => e.Description == "Updated reverb effect")), Times.Once);
        dataServiceMock.Verify(m => m.UpdateEffectByNameAsync("Delay", It.Is<Effect>(e => e.Description == "Updated delay effect")), Times.Once);
        dataServiceMock.Verify(m => m.CreateEffectAsync(It.IsAny<string>(), It.IsAny<Effect>()), Times.Never);
    }

    [Fact]
    public async Task UploadEffects_MixedEffects_CreatesAndUpdates()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MidiEndpointsHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();

        var effects = new List<Effect>
        {
            new() { Name = "Reverb", Description = "Updated reverb effect" },
            new() { Name = "Delay", Description = "New delay effect" },
            new() { Name = "Chorus", Description = "New chorus effect" }
        };

        var effectsOptions = Options.Create(new EffectsOptions { Effects = effects });

        // Only Reverb exists in database
        var existingEffects = new List<Effect>
        {
            new() { Name = "Reverb", Description = "Old reverb effect" }
        };

        dataServiceMock.Setup(m => m.GetAllEffectsAsync()).ReturnsAsync(existingEffects);

        dataServiceMock.Setup(m => m.UpdateEffectByNameAsync("Reverb", It.IsAny<Effect>())).Returns(Task.CompletedTask);
        dataServiceMock.Setup(m => m.CreateEffectAsync("Delay", It.IsAny<Effect>())).Returns(Task.CompletedTask);
        dataServiceMock.Setup(m => m.CreateEffectAsync("Chorus", It.IsAny<Effect>())).Returns(Task.CompletedTask);

        // Act
        var result = await InvokeUploadEffectsAsync(loggerMock.Object, dataServiceMock.Object, effectsOptions);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(200, status);
        Assert.Contains("Created: 2", body);
        Assert.Contains("Updated: 1", body);

        dataServiceMock.Verify(m => m.GetAllEffectsAsync(), Times.Once);
        dataServiceMock.Verify(m => m.UpdateEffectByNameAsync("Reverb", It.IsAny<Effect>()), Times.Once);
        dataServiceMock.Verify(m => m.CreateEffectAsync("Delay", It.IsAny<Effect>()), Times.Once);
        dataServiceMock.Verify(m => m.CreateEffectAsync("Chorus", It.IsAny<Effect>()), Times.Once);
    }

    [Fact]
    public async Task UploadEffects_DatabaseEmpty_CreatesAll()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MidiEndpointsHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();

        var effects = new List<Effect>
        {
            new() { Name = "Reverb", Description = "Reverb effect" }
        };

        var effectsOptions = Options.Create(new EffectsOptions { Effects = effects });

        // GetAllEffectsAsync throws (database doesn't exist or is empty)
        dataServiceMock.Setup(m => m.GetAllEffectsAsync()).ThrowsAsync(new Exception("Database empty"));

        dataServiceMock.Setup(m => m.CreateEffectAsync("Reverb", It.IsAny<Effect>())).Returns(Task.CompletedTask);

        // Act
        var result = await InvokeUploadEffectsAsync(loggerMock.Object, dataServiceMock.Object, effectsOptions);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(200, status);
        Assert.Contains("Created: 1", body);

        dataServiceMock.Verify(m => m.GetAllEffectsAsync(), Times.Once);
        dataServiceMock.Verify(m => m.CreateEffectAsync("Reverb", It.IsAny<Effect>()), Times.Once);
    }

    [Fact]
    public async Task UploadEffects_CreateFails_Returns500WithErrors()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MidiEndpointsHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();

        var effects = new List<Effect>
        {
            new() { Name = "Reverb", Description = "Reverb effect" },
            new() { Name = "Delay", Description = "Delay effect" }
        };

        var effectsOptions = Options.Create(new EffectsOptions { Effects = effects });

        dataServiceMock.Setup(m => m.GetAllEffectsAsync()).ReturnsAsync(new List<Effect>());

        dataServiceMock.Setup(m => m.CreateEffectAsync("Reverb", It.IsAny<Effect>()))
            .ThrowsAsync(new Exception("Create failed"));
        dataServiceMock.Setup(m => m.CreateEffectAsync("Delay", It.IsAny<Effect>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await InvokeUploadEffectsAsync(loggerMock.Object, dataServiceMock.Object, effectsOptions);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(500, status);
        Assert.Contains("Effects upload completed with errors", body);
        Assert.Contains("Created: 1", body);
        Assert.Contains("Failed: 1", body);
        Assert.Contains("Reverb", body);

        dataServiceMock.Verify(m => m.CreateEffectAsync("Reverb", It.IsAny<Effect>()), Times.Once);
        dataServiceMock.Verify(m => m.CreateEffectAsync("Delay", It.IsAny<Effect>()), Times.Once);
    }

    [Fact]
    public async Task UploadEffects_UpdateFails_Returns500WithErrors()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MidiEndpointsHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();

        var effects = new List<Effect>
        {
            new() { Name = "Reverb", Description = "Updated reverb effect" }
        };

        var effectsOptions = Options.Create(new EffectsOptions { Effects = effects });

        var existingEffects = new List<Effect>
        {
            new() { Name = "Reverb", Description = "Old reverb effect" }
        };

        dataServiceMock.Setup(m => m.GetAllEffectsAsync()).ReturnsAsync(existingEffects);

        dataServiceMock.Setup(m => m.UpdateEffectByNameAsync("Reverb", It.IsAny<Effect>()))
            .ThrowsAsync(new Exception("Update failed"));

        // Act
        var result = await InvokeUploadEffectsAsync(loggerMock.Object, dataServiceMock.Object, effectsOptions);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(500, status);
        Assert.Contains("Effects upload completed with errors", body);
        Assert.Contains("Failed: 1", body);
        Assert.Contains("Reverb", body);

        dataServiceMock.Verify(m => m.UpdateEffectByNameAsync("Reverb", It.IsAny<Effect>()), Times.Once);
    }

    [Fact]
    public async Task UploadEffects_CaseInsensitiveMatch_Updates()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<MidiEndpointsHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();

        var effects = new List<Effect>
        {
            new() { Name = "REVERB", Description = "Updated reverb effect" }
        };

        var effectsOptions = Options.Create(new EffectsOptions { Effects = effects });

        // Existing effect with different case
        var existingEffects = new List<Effect>
        {
            new() { Name = "reverb", Description = "Old reverb effect" }
        };

        dataServiceMock.Setup(m => m.GetAllEffectsAsync()).ReturnsAsync(existingEffects);

        dataServiceMock.Setup(m => m.UpdateEffectByNameAsync("REVERB", It.IsAny<Effect>())).Returns(Task.CompletedTask);

        // Act
        var result = await InvokeUploadEffectsAsync(loggerMock.Object, dataServiceMock.Object, effectsOptions);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(200, status);
        Assert.Contains("Updated: 1", body);
        Assert.Contains("Created: 0", body);

        dataServiceMock.Verify(m => m.UpdateEffectByNameAsync("REVERB", It.IsAny<Effect>()), Times.Once);
        dataServiceMock.Verify(m => m.CreateEffectAsync(It.IsAny<string>(), It.IsAny<Effect>()), Times.Never);
    }
}

