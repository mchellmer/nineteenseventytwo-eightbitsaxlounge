using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Handlers;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Models;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.UnitTests.EndpointTests;

public class InitializeDataModelHandlerTests : TestBase
{
    [Fact]
    public async Task InitializeDataModel_Success_Returns200()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<InitializeDataModelHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();

        var dbNames = new List<string> { "devices", "effects", "selectors" };
        var databaseOptions = Options.Create(new DatabaseOptions { Names = dbNames });

        dataServiceMock.Setup(m => m.CreateDatabaseAsync("devices")).Returns(Task.CompletedTask);
        dataServiceMock.Setup(m => m.CreateDatabaseAsync("effects")).Returns(Task.CompletedTask);
        dataServiceMock.Setup(m => m.CreateDatabaseAsync("selectors")).Returns(Task.CompletedTask);

        var handler = new InitializeDataModelHandler(loggerMock.Object, dataServiceMock.Object, databaseOptions);

        // Act
        var result = await handler.HandleAsync();
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
        var loggerMock = new Mock<ILogger<InitializeDataModelHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();

        var dbNames = new List<string> { "devices", "effects", "selectors" };
        var databaseOptions = Options.Create(new DatabaseOptions { Names = dbNames });

        dataServiceMock.Setup(m => m.CreateDatabaseAsync("devices"))
            .ThrowsAsync(new Exception("Database creation failed"));

        var handler = new InitializeDataModelHandler(loggerMock.Object, dataServiceMock.Object, databaseOptions);

        // Act
        var result = await handler.HandleAsync();
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(500, status);
        Assert.Contains("Data model initialization failed", body);
        Assert.Contains("devices", body);

        dataServiceMock.Verify(m => m.CreateDatabaseAsync("devices"), Times.Once);
    }
}
