using Microsoft.Extensions.Logging;
using Moq;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Handlers;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Models;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.UnitTests.EndpointTests;

public class SendControlChangeMessageHandlerTests : TestBase
{
    private const string TestMidiConnectName = "TestMidiConnect";

    [Fact]
    public async Task SendControlChangeMessage_Success_Returns200()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<SendControlChangeMessageHandler>>();
        var deviceServiceMock = new Mock<IMidiDeviceService>();

        ControlChangeMessage? sentMessage = null;
        deviceServiceMock.Setup(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync(TestMidiConnectName, It.IsAny<ControlChangeMessage>()))
            .Callback<string, ControlChangeMessage>((_, msg) => sentMessage = msg)
            .Returns((string _, ControlChangeMessage m) => Task.FromResult(m));

        var handler = new SendControlChangeMessageHandler(loggerMock.Object, deviceServiceMock.Object);
        int address = 10;
        int value = 64;
        var request = new SendControlChangeMessageRequest(TestMidiConnectName, address, value);

        // Act
        var result = await handler.HandleAsync(request);
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
        var loggerMock = new Mock<ILogger<SendControlChangeMessageHandler>>();
        var deviceServiceMock = new Mock<IMidiDeviceService>();

        deviceServiceMock.Setup(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync(TestMidiConnectName, It.IsAny<ControlChangeMessage>()))
            .ThrowsAsync(new Exception("Native send failure"));

        var handler = new SendControlChangeMessageHandler(loggerMock.Object, deviceServiceMock.Object);
        var request = new SendControlChangeMessageRequest(TestMidiConnectName, 7, 127);

        // Act
        var result = await handler.HandleAsync(request);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(500, status);
        Assert.Contains("Failed to send Control Change Message", body);
        deviceServiceMock.Verify(m => m.SendControlChangeMessageByDeviceMidiConnectNameAsync(TestMidiConnectName, It.IsAny<ControlChangeMessage>()), Times.Once);
    }
}
