using Microsoft.Extensions.Logging;
using Moq;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Handlers;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.UnitTests.EndpointTests;

public class UploadDeviceHandlerTests : TestBase
{
    [Fact]
    public async Task UploadDevice_VentrisDualReverbJson_IsValid()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UploadDeviceHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();
        
        // Mock data service to return existing effects for the Ventris Dual Reverb upload
        dataServiceMock.Setup(m => m.GetEffectByNameAsync(It.IsAny<string>()))
            .ReturnsAsync((string name) => new Effect { Name = name, Description = "Test Description" });
        
        dataServiceMock.Setup(m => m.GetDeviceByNameAsync(It.IsAny<string>())).ReturnsAsync((MidiDevice)null);
        dataServiceMock.Setup(m => m.GetSelectorByNameAsync(It.IsAny<string>())).ReturnsAsync((Selector)null);

        var handler = new UploadDeviceHandler(loggerMock.Object, dataServiceMock.Object);

        // Act
        var result = await handler.HandleAsync("VentrisDualReverb");
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(200, status);
        Assert.Contains("uploaded successfully", body);
    }

    [Fact]
    public async Task UploadDevice_DeviceDoesNotExist_CreatesNewDevice()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UploadDeviceHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();
        var deviceName = "NewDevice";
        var filePath = $"appsettings.Devices.{deviceName}.json";

        var jsonContent = @"{
            ""Devices"": [{ ""Name"": ""NewDevice"", ""Description"": ""New Desc"", ""MidiConnectName"": ""Conn"", ""MidiImplementation"": [], ""DeviceEffects"": [] }]
        }";
        await File.WriteAllTextAsync(filePath, jsonContent);

        try
        {
            // Mock GetDeviceByNameAsync to return null (device doesn't exist)
            dataServiceMock.Setup(m => m.GetDeviceByNameAsync(deviceName)).ReturnsAsync((MidiDevice)null);
            dataServiceMock.Setup(m => m.CreateDeviceAsync(It.IsAny<MidiDevice>())).Returns(Task.CompletedTask);

            var handler = new UploadDeviceHandler(loggerMock.Object, dataServiceMock.Object);

            // Act
            var result = await handler.HandleAsync(deviceName);
            var (status, body) = await ExecuteResultAsync(result);

            // Assert
            Assert.Equal(200, status);
            dataServiceMock.Verify(m => m.CreateDeviceAsync(It.Is<MidiDevice>(d => d.Name == deviceName)), Times.Once);
            dataServiceMock.Verify(m => m.UpdateDeviceByNameAsync(It.IsAny<string>(), It.IsAny<MidiDevice>()), Times.Never);
        }
        finally
        {
            if (File.Exists(filePath)) File.Delete(filePath);
        }
    }

    [Fact]
    public async Task UploadDevice_FileNotFound_Returns404()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UploadDeviceHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();
        var deviceName = "NonExistentDevice";

        var handler = new UploadDeviceHandler(loggerMock.Object, dataServiceMock.Object);

        // Act
        var result = await handler.HandleAsync(deviceName);
        var (status, body) = await ExecuteResultAsync(result);

        // Assert
        Assert.Equal(404, status);
        Assert.Contains("Configuration file for device", body);
        Assert.Contains(deviceName, body);
    }

    [Fact]
    public async Task UploadDevice_ValidJson_UpsertsAllEntities()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UploadDeviceHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();
        var deviceName = "TestDeviceUpload";
        var fileName = $"appsettings.Devices.{deviceName}.json";

        var jsonContent = @"{
              ""Devices"": [
                {
                  ""Name"": ""TestDeviceUpload"",
                  ""MidiConnectName"": ""Test Midi Connect"",
                  ""Description"": ""Test Description"",
                  ""Active"": true,
                  ""MidiImplementation"": [],
                  ""DeviceEffects"": []
                }
              ],
              ""Effects"": [
                {
                  ""Name"": ""TestEffect"",
                  ""Description"": ""Updated Description"",
                  ""DeviceSettings"": [
                    {
                      ""Name"": ""Control1"",
                      ""DeviceName"": ""TestDeviceUpload"",
                      ""EffectName"": ""TestEffect"",
                      ""DeviceMidiImplementationName"": ""Implementation1""
                    }
                  ]
                }
              ],
              ""Selectors"": [
                {
                  ""Name"": ""TestSelector"",
                  ""Selections"": [
                    {
                      ""Name"": ""Selection1"",
                      ""ControlChangeMessageValue"": 1
                    }
                  ]
                }
              ]
            }";

        await File.WriteAllTextAsync(fileName, jsonContent);

        try
        {
            // Setup for Devices: existing one to update
            dataServiceMock.Setup(m => m.GetDeviceByNameAsync("TestDeviceUpload"))
                .ReturnsAsync(new MidiDevice 
                { 
                    Name = "TestDeviceUpload", 
                    Description = "Old", 
                    MidiConnectName = "Old", 
                    MidiImplementation = new List<MidiConfiguration>(),
                    DeviceEffects = new List<DeviceEffect>()
                });
            
            // Setup for Selectors: new one to create
            dataServiceMock.Setup(m => m.GetSelectorByNameAsync("TestSelector"))
                .ReturnsAsync((Selector)null);

            // Setup for Effects: existing one to update/merge
            var existingEffect = new Effect { Name = "TestEffect", Description = "Old Description", DeviceSettings = new List<DeviceSetting>() };
            dataServiceMock.Setup(m => m.GetEffectByNameAsync("TestEffect"))
                .ReturnsAsync(existingEffect);

            var handler = new UploadDeviceHandler(loggerMock.Object, dataServiceMock.Object);

            // Act
            var result = await handler.HandleAsync(deviceName);
            var (status, body) = await ExecuteResultAsync(result);

            // Assert
            Assert.Equal(200, status);
            Assert.Contains("configuration uploaded successfully", body);
            Assert.Contains(deviceName, body);

            // Verify Device Update
            dataServiceMock.Verify(m => m.UpdateDeviceByNameAsync("TestDeviceUpload", It.IsAny<MidiDevice>()), Times.Once);
            dataServiceMock.Verify(m => m.CreateDeviceAsync(It.IsAny<MidiDevice>()), Times.Never);

            // Verify Selector Creation
            dataServiceMock.Verify(m => m.CreateSelectorAsync("TestSelector", It.IsAny<Selector>()), Times.Once);

            // Verify Effect Update/Merge
            dataServiceMock.Verify(m => m.UpdateEffectByNameAsync("TestEffect", It.Is<Effect>(e => 
                e.Description == "Updated Description" && 
                e.DeviceSettings.Count == 1 && 
                e.DeviceSettings[0].Name == "Control1" &&
                e.DeviceSettings[0].DeviceName == "TestDeviceUpload")), Times.Once);
        }
        finally
        {
            if (File.Exists(fileName)) File.Delete(fileName);
        }
    }

    [Fact]
    public async Task UploadDevice_EffectNotFound_ThrowsError()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UploadDeviceHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();
        var deviceName = "EffectNotFoundDevice";
        var fileName = $"appsettings.Devices.{deviceName}.json";

        var jsonContent = @"{
              ""Effects"": [
                {
                  ""Name"": ""NonExistentEffect"",
                  ""Description"": ""Updated Description""
                }
              ]
            }";

        await File.WriteAllTextAsync(fileName, jsonContent);

        try
        {
            // Mock GetEffectByNameAsync to return null (simulating new implementation behavior)
            dataServiceMock.Setup(m => m.GetEffectByNameAsync("NonExistentEffect"))
                .ReturnsAsync((Effect?)null);

            var handler = new UploadDeviceHandler(loggerMock.Object, dataServiceMock.Object);

            // Act
            var result = await handler.HandleAsync(deviceName);
            var (status, body) = await ExecuteResultAsync(result);

            // Assert
            // Now this should FAIL with 500 because the handler throws an error when an effect is not found
            Assert.Equal(500, status);
            Assert.Contains("The following effects were not found", body);
            Assert.Contains("NonExistentEffect", body);
            dataServiceMock.Verify(m => m.UpdateEffectByNameAsync(It.IsAny<string>(), It.IsAny<Effect>()), Times.Never);
        }
        finally
        {
            if (File.Exists(fileName)) File.Delete(fileName);
        }
    }

    [Fact]
    public async Task UploadDevice_InvalidJson_Returns500()
    {
        // Arrange
        var loggerMock = new Mock<ILogger<UploadDeviceHandler>>();
        var dataServiceMock = new Mock<IMidiDataService>();
        var deviceName = "InvalidDevice";
        var fileName = $"appsettings.Devices.{deviceName}.json";

        await File.WriteAllTextAsync(fileName, "{ invalid json }");

        try
        {
            var handler = new UploadDeviceHandler(loggerMock.Object, dataServiceMock.Object);

            // Act
            var result = await handler.HandleAsync(deviceName);
            var (status, body) = await ExecuteResultAsync(result);

            // Assert
            Assert.Equal(500, status);
            Assert.Contains("Device upload failed", body);
        }
        finally
        {
            if (File.Exists(fileName)) File.Delete(fileName);
        }
    }
}
