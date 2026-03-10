using Microsoft.Extensions.Logging;
using Xunit;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Handlers;
using Moq;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.UnitTests.EndpointTests;

public class HandlerHelperTests
{
    [Fact]
    public void ScaleFrom127ToBase_ReturnsZeroForZeroInput()
    {
        var helper = new HandlerHelper(Mock.Of<ILogger>(), Mock.Of<IMidiDeviceService>(), Mock.Of<IMidiDataService>());
        Assert.Equal(0, helper.ScaleFrom127ToBase(0, 10));
    }

    [Fact]
    public void ScaleFrom127ToBase_ReturnsTargetFor127Input()
    {
        var helper = new HandlerHelper(Mock.Of<ILogger>(), Mock.Of<IMidiDeviceService>(), Mock.Of<IMidiDataService>());
        Assert.Equal(10, helper.ScaleFrom127ToBase(127, 10));
    }

    [Fact]
    public void ScaleFrom127ToBase_ClampsBelowZero()
    {
        var helper = new HandlerHelper(Mock.Of<ILogger>(), Mock.Of<IMidiDeviceService>(), Mock.Of<IMidiDataService>());
        Assert.Equal(0, helper.ScaleFrom127ToBase(-5, 10));
    }

    [Fact]
    public void ScaleFrom127ToBase_ClampsAbove127()
    {
        var helper = new HandlerHelper(Mock.Of<ILogger>(), Mock.Of<IMidiDeviceService>(), Mock.Of<IMidiDataService>());
        Assert.Equal(10, helper.ScaleFrom127ToBase(200, 10));
    }

    [Fact]
    public void ScaleFrom127ToBase_RoundsProportionally()
    {
        var helper = new HandlerHelper(Mock.Of<ILogger>(), Mock.Of<IMidiDeviceService>(), Mock.Of<IMidiDataService>());
        // 63.5 ~= 64 should map roughly to 5 for base 10
        var scaled = helper.ScaleFrom127ToBase(64, 10);
        Assert.InRange(scaled, 4, 6);
    }
}

