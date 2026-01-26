using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using Moq;

using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models.Winmm;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.UnitTests;

public class WinmmMidiDeviceServiceTests
{
    private static IConfiguration CreateConfiguration(string? proxyUrl = null)
    {
        var configDict = new Dictionary<string, string?>();
        if (!string.IsNullOrWhiteSpace(proxyUrl))
        {
            configDict["MidiDeviceService:Url"] = proxyUrl;
        }
        return new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();
    }

    private static WinmmMidiDeviceService BuildService(
        Mock<IMidiOutDeviceFactory> factoryMock,
        Mock<IMidiOutDevice> deviceMock,
        Mock<ILogger<WinmmMidiDeviceService>> loggerMock,
        string? proxyUrl = null,
        HttpClient? httpClient = null)
    {
        factoryMock.Setup(f => f.Create(It.IsAny<string>())).Returns(deviceMock.Object);
        var config = CreateConfiguration(proxyUrl);
        return new WinmmMidiDeviceService(loggerMock.Object, factoryMock.Object, config, httpClient);
    }

    [Fact]
    public async Task SendControlChangeMessage_Success_CallsSend()
    {
        var loggerMock = new Mock<ILogger<WinmmMidiDeviceService>>();
        var factoryMock = new Mock<IMidiOutDeviceFactory>();
        var deviceMock = new Mock<IMidiOutDevice>();

        deviceMock.Setup(d => d.TryOpen()).Returns((true, MmResult.NoError, null));
        deviceMock.Setup(d => d.TrySendControlChangeMessageDetailedAsync(10, 64, true, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((true, MmResult.NoError, (string?)null));

        var service = BuildService(factoryMock, deviceMock, loggerMock);

        await service.SendControlChangeMessageByDeviceMidiConnectNameAsync("TestConnect", new ControlChangeMessage { Address = 10, Value = 64 });

        deviceMock.Verify(d => d.TryOpen(), Times.Once);
        deviceMock.Verify(d => d.TrySendControlChangeMessageDetailedAsync(10, 64, true, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendControlChangeMessage_OpenFails_Throws()
    {
        var loggerMock = new Mock<ILogger<WinmmMidiDeviceService>>();
        var factoryMock = new Mock<IMidiOutDeviceFactory>();
        var deviceMock = new Mock<IMidiOutDevice>();

        deviceMock.Setup(d => d.TryOpen()).Returns((false, MmResult.UnspecError, "Open error"));

        var service = BuildService(factoryMock, deviceMock, loggerMock);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.SendControlChangeMessageByDeviceMidiConnectNameAsync("TestConnect", new ControlChangeMessage { Address = 10, Value = 64 }));
        Assert.Contains("Open error", ex.Message);
        deviceMock.Verify(d => d.TryOpen(), Times.Once);
        deviceMock.Verify(d => d.TrySendControlChangeMessageDetailedAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>(), It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SendControlChangeMessage_SendFails_Throws()
    {
        var loggerMock = new Mock<ILogger<WinmmMidiDeviceService>>();
        var factoryMock = new Mock<IMidiOutDeviceFactory>();
        var deviceMock = new Mock<IMidiOutDevice>();

        deviceMock.Setup(d => d.TryOpen()).Returns((true, MmResult.NoError, null));
        deviceMock.Setup(d => d.TrySendControlChangeMessageDetailedAsync(10, 64, true, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((false, MmResult.InvalidHandle, "Send error"));

        var service = BuildService(factoryMock, deviceMock, loggerMock);

        var ex = await Assert.ThrowsAsync<Exception>(() => service.SendControlChangeMessageByDeviceMidiConnectNameAsync("TestConnect", new ControlChangeMessage { Address = 10, Value = 64 }));
        Assert.Contains("Send error", ex.Message);
        deviceMock.Verify(d => d.TryOpen(), Times.Once);
        deviceMock.Verify(d => d.TrySendControlChangeMessageDetailedAsync(10, 64, true, It.IsAny<int>(), It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void SendControlChangeMessage_ProxyConfigured_RoutesToProxyAsync()
    {
        var loggerMock = new Mock<ILogger<WinmmMidiDeviceService>>();
        var factoryMock = new Mock<IMidiOutDeviceFactory>();
        var deviceMock = new Mock<IMidiOutDevice>();

        var handlerMock = new Mock<HttpMessageHandler>();
        var httpClient = new HttpClient(handlerMock.Object);

        var service = BuildService(factoryMock, deviceMock, loggerMock, "https://localhost:7063", httpClient);

        // Service should be configured for proxy
        Assert.True(service.IsProxyEnabled);
        Assert.Equal("https://localhost:7063", service.ProxyUrl);
    }
}



