using Microsoft.Extensions.Logging;
using Moq;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.UnitTests;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.UnitTests.DataTests;

public class EightBitSaxLoungeMidiDataServiceTests
{
    private readonly Mock<IEffectActivatorFactory> _activatorFactoryMock;
    private readonly Mock<IDataAccess> _dataAccessMock;
    private readonly Mock<ILogger<EightBitSaxLoungeMidiDataService>> _loggerMock;
    private readonly EightBitSaxLoungeMidiDataService _service;

    public EightBitSaxLoungeMidiDataServiceTests()
    {
        _activatorFactoryMock = new Mock<IEffectActivatorFactory>();
        _dataAccessMock = new Mock<IDataAccess>();
        _loggerMock = new Mock<ILogger<EightBitSaxLoungeMidiDataService>>();
        _service = new EightBitSaxLoungeMidiDataService(
            _activatorFactoryMock.Object,
            _dataAccessMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task CreateDatabaseAsync_CallsSaveDataAsync()
    {
        // Arrange
        var dbName = "testdb";

        // Act
        await _service.CreateDatabaseAsync(dbName);

        // Assert
        _dataAccessMock.Verify(m => m.SaveDataAsync(
            "PUT",
            It.Is<EightBitSaxLoungeDataRequest>(r => r.RequestRoute == dbName && r.RequestBody == null),
            "EightBitSaxLoungeDataLayer"), Times.Once);
    }

    [Fact]
    public async Task CreateDeviceAsync_CallsSaveDataAsync()
    {
        // Arrange
        var device = TestObjects.Clone(TestObjects.PrototypeDevice);

        // Act
        await _service.CreateDeviceAsync(device);

        // Assert
        _dataAccessMock.Verify(m => m.SaveDataAsync(
            "POST",
            It.Is<EightBitSaxLoungeDataRequest>(r => r.RequestRoute == "devices" && r.RequestBody == device),
            "EightBitSaxLoungeDataLayer"), Times.Once);
    }

    [Fact]
    public async Task GetDeviceByNameAsync_ReturnsDevice_WhenFound()
    {
        // Arrange
        var deviceName = "TestDevice";
        var devices = new List<MidiDevice>
        {
            TestObjects.BuildDevice(deviceName)
        };
        _dataAccessMock.Setup(m => m.LoadDataAsync<MidiDevice, EightBitSaxLoungeDataRequest>(
            "GET", It.Is<EightBitSaxLoungeDataRequest>(r => r.RequestRoute == $"devices/{deviceName}"), "EightBitSaxLoungeDataLayer"))
            .ReturnsAsync(devices);

        // Act
        var result = await _service.GetDeviceByNameAsync(deviceName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(deviceName, result.Name);
    }

    [Fact]
    public async Task GetDeviceByNameAsync_ReturnsNull_WhenNotFound()
    {
        // Arrange
        _dataAccessMock.Setup(m => m.LoadDataAsync<MidiDevice, EightBitSaxLoungeDataRequest>(
            "GET", It.IsAny<EightBitSaxLoungeDataRequest>(), "EightBitSaxLoungeDataLayer"))
            .ThrowsAsync(new Exception("404 Not Found"));

        // Act
        var result = await _service.GetDeviceByNameAsync("NonExistent");

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateDeviceEffectActiveStateAsync_UpdatesAndSaves()
    {
        // Arrange
        var deviceName = "TestDevice";
        var effectName = "TestEffect";
        var device = TestObjects.BuildDevice(deviceName, new List<DeviceEffect> { TestObjects.BuildEffect(effectName, false, new List<DeviceEffectSetting>()) });
        _dataAccessMock.Setup(m => m.LoadDataAsync<MidiDevice, EightBitSaxLoungeDataRequest>(
            "GET", It.Is<EightBitSaxLoungeDataRequest>(r => r.RequestRoute == $"devices/{deviceName}"), "EightBitSaxLoungeDataLayer"))
            .ReturnsAsync(new List<MidiDevice> { device });

        // Act
        await _service.UpdateDeviceEffectActiveStateAsync(deviceName, effectName, true);

        // Assert
        Assert.True(device.DeviceEffects[0].Active);
        _dataAccessMock.Verify(m => m.SaveDataAsync(
            "PUT",
            It.Is<EightBitSaxLoungeDataRequest>(r => r.RequestRoute == $"devices/{deviceName}"),
            "EightBitSaxLoungeDataLayer"), Times.Once);
    }

    [Fact]
    public async Task GetEffectByNameAsync_ReturnsEffect_WhenFound()
    {
        // Arrange
        var effectName = "TestEffect";
        var effects = new List<Effect>
        {
            new Effect { Name = effectName, Description = "Desc" }
        };
        _dataAccessMock.Setup(m => m.LoadDataAsync<Effect, EightBitSaxLoungeDataRequest>(
            "GET", It.Is<EightBitSaxLoungeDataRequest>(r => r.RequestRoute == $"effects/{effectName}"), "EightBitSaxLoungeDataLayer"))
            .ReturnsAsync(effects);

        // Act
        var result = await _service.GetEffectByNameAsync(effectName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(effectName, result.Name);
    }

    [Fact]
    public async Task GetSelectorByNameAsync_ReturnsSelector_WhenFound()
    {
        // Arrange
        var selectorName = "TestSelector";
        var selectors = new List<Selector>
        {
            new Selector { Name = selectorName, Selections = [] }
        };
        _dataAccessMock.Setup(m => m.LoadDataAsync<Selector, EightBitSaxLoungeDataRequest>(
            "GET", It.Is<EightBitSaxLoungeDataRequest>(r => r.RequestRoute == $"selectors/{selectorName}"), "EightBitSaxLoungeDataLayer"))
            .ReturnsAsync(selectors);

        // Act
        var result = await _service.GetSelectorByNameAsync(selectorName);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(selectorName, result.Name);
    }

    [Fact]
    public async Task GetControlChangeMessageToActivateDeviceEffectAsync_CallsActivator()
    {
        // Arrange
        var deviceName = "TestDevice";
        var effectName = "TestEffect";
        var device = new MidiDevice { Name = deviceName, Description = "Desc", MidiConnectName = "Conn", MidiImplementation = [], DeviceEffects = [] };
        var expectedMsg = new ControlChangeMessage { Address = 10, Value = 127 };

        _dataAccessMock.Setup(m => m.LoadDataAsync<MidiDevice, EightBitSaxLoungeDataRequest>(
            "GET", It.Is<EightBitSaxLoungeDataRequest>(r => r.RequestRoute == $"devices/{deviceName}"), "EightBitSaxLoungeDataLayer"))
            .ReturnsAsync(new List<MidiDevice> { device });

        var activatorMock = new Mock<IEffectActivator>();
        activatorMock.Setup(m => m.BuildActivationMessageAsync(device, effectName, true))
            .ReturnsAsync(expectedMsg);

        _activatorFactoryMock.Setup(m => m.GetActivator(deviceName)).Returns(activatorMock.Object);

        // Act
        var result = await _service.GetControlChangeMessageToActivateDeviceEffectAsync(deviceName, effectName, true);

        // Assert
        Assert.Equal(expectedMsg, result);
    }

    [Fact]
    public async Task GetControlChangeMessageToSetDeviceEffectSettingAsync_ReturnsCorrectMessage()
    {
        // Arrange
        var deviceName = "TestDevice";
        var effectName = "TestEffect";
        var settingName = "TestSetting";
        var device = new MidiDevice
        {
            Name = deviceName,
            Description = "Desc",
            MidiConnectName = "Conn",
            MidiImplementation = new List<MidiConfiguration>
            {
                new MidiConfiguration
                {
                    Name = settingName,
                    ControlChangeAddresses = new List<ControlChangeAddress>
                    {
                        new ControlChangeAddress { Name = effectName, Value = 20 }
                    }
                }
            },
            DeviceEffects = new List<DeviceEffect>
            {
                new DeviceEffect
                {
                    Name = effectName,
                    Active = true,
                    EffectSettings = new List<DeviceEffectSetting>
                    {
                        new DeviceEffectSetting { Name = settingName, Value = 0, DefaultValue = 0 }
                    }
                }
            }
        };

        _dataAccessMock.Setup(m => m.LoadDataAsync<MidiDevice, EightBitSaxLoungeDataRequest>(
            "GET", It.Is<EightBitSaxLoungeDataRequest>(r => r.RequestRoute == $"devices/{deviceName}"), "EightBitSaxLoungeDataLayer"))
            .ReturnsAsync(new List<MidiDevice> { device });

        // Act
        var result = await _service.GetControlChangeMessageToSetDeviceEffectSettingAsync(deviceName, effectName, settingName, 64);

        // Assert
        Assert.Equal(20, result.Address);
        Assert.Equal(64, result.Value);
    }

    [Fact]
    public async Task GetControlChangeMessageToSetDeviceEffectSettingAsync_WithDependency_ReturnsCorrectMessage()
    {
        // Arrange
        var deviceName = "TestDevice";
        var effectName = "TestEffect";
        var settingName = "Control1";
        var dependencyName = "ReverbEngine";
        var dependentEffectName = "Room";
        var implementationName = "EngineParameter1";

        var device = new MidiDevice
        {
            Name = deviceName,
            Description = "Desc",
            MidiConnectName = "Conn",
            MidiImplementation = new List<MidiConfiguration>
            {
                new MidiConfiguration
                {
                    Name = implementationName,
                    ControlChangeAddresses = new List<ControlChangeAddress>
                    {
                        new ControlChangeAddress { Name = effectName, Value = 15 }
                    }
                }
            },
            DeviceEffects = new List<DeviceEffect>
            {
                new DeviceEffect
                {
                    Name = effectName,
                    Active = true,
                    EffectSettings = new List<DeviceEffectSetting>
                    {
                        new DeviceEffectSetting
                        {
                            Name = settingName,
                            Value = 0,
                            DefaultValue = 0,
                            DeviceEffectSettingDependencyName = dependencyName
                        },
                        new DeviceEffectSetting
                        {
                            Name = dependencyName,
                            Value = 0,
                            DefaultValue = 0
                        }
                    }
                }
            }
        };

        var selector = new Selector
        {
            Name = dependencyName,
            Selections = new List<MidiSelection>
            {
                new MidiSelection { Name = dependentEffectName, ControlChangeMessageValue = 0 }
            }
        };

        var dependentEffect = new Effect
        {
            Name = dependentEffectName,
            Description = "Desc",
            DeviceSettings = new List<DeviceSetting>
            {
                new DeviceSetting
                {
                    Name = settingName,
                    DeviceName = deviceName,
                    EffectName = dependentEffectName,
                    DeviceMidiImplementationName = implementationName
                }
            }
        };

        _dataAccessMock.Setup(m => m.LoadDataAsync<MidiDevice, EightBitSaxLoungeDataRequest>(
            "GET", It.Is<EightBitSaxLoungeDataRequest>(r => r.RequestRoute == $"devices/{deviceName}"), "EightBitSaxLoungeDataLayer"))
            .ReturnsAsync(new List<MidiDevice> { device });

        _dataAccessMock.Setup(m => m.LoadDataAsync<Selector, EightBitSaxLoungeDataRequest>(
            "GET", It.Is<EightBitSaxLoungeDataRequest>(r => r.RequestRoute == $"selectors/{dependencyName}"), "EightBitSaxLoungeDataLayer"))
            .ReturnsAsync(new List<Selector> { selector });

        _dataAccessMock.Setup(m => m.LoadDataAsync<Effect, EightBitSaxLoungeDataRequest>(
            "GET", It.Is<EightBitSaxLoungeDataRequest>(r => r.RequestRoute == $"effects/{dependentEffectName}"), "EightBitSaxLoungeDataLayer"))
            .ReturnsAsync(new List<Effect> { dependentEffect });

        // Act
        var result = await _service.GetControlChangeMessageToSetDeviceEffectSettingAsync(deviceName, effectName, settingName, 100);

        // Assert
        Assert.Equal(15, result.Address);
        Assert.Equal(100, result.Value);
    }

    [Fact]
    public async Task GetControlChangeMessageToSetDeviceEffectSettingAsync_VentrisControl1Dependency_ReturnsCorrectMessage()
    {
        // Arrange
        var deviceName = "VentrisDualReverb";
        var effectName = "ReverbEngineA";
        var settingName = "Control1";
        var dependencyName = "ReverbEngine";
        var dependentEffectName = "Room";
        var implementationName = "EngineParameter1";

        var device = new MidiDevice
        {
            Name = deviceName,
            Description = "Desc",
            MidiConnectName = "Conn",
            MidiImplementation = new List<MidiConfiguration>
            {
                new MidiConfiguration
                {
                    Name = implementationName,
                    ControlChangeAddresses = new List<ControlChangeAddress>
                    {
                        new ControlChangeAddress { Name = effectName, Value = 15 }
                    }
                }
            },
            DeviceEffects = new List<DeviceEffect>
            {
                new DeviceEffect
                {
                    Name = effectName,
                    Active = true,
                    EffectSettings = new List<DeviceEffectSetting>
                    {
                        new DeviceEffectSetting
                        {
                            Name = settingName,
                            Value = 0,
                            DefaultValue = 0,
                            DeviceEffectSettingDependencyName = dependencyName
                        },
                        new DeviceEffectSetting
                        {
                            Name = dependencyName,
                            Value = 10, // Value 10 corresponds to "Room" in selector
                            DefaultValue = 0
                        }
                    }
                }
            }
        };

        var selector = new Selector
        {
            Name = dependencyName,
            Selections = new List<MidiSelection>
            {
                new MidiSelection { Name = dependentEffectName, ControlChangeMessageValue = 10 }
            }
        };

        var dependentEffect = new Effect
        {
            Name = dependentEffectName,
            Description = "Desc",
            DeviceSettings = new List<DeviceSetting>
            {
                new DeviceSetting
                {
                    Name = settingName,
                    DeviceName = deviceName,
                    EffectName = dependentEffectName,
                    DeviceMidiImplementationName = implementationName
                }
            }
        };

        _dataAccessMock.Setup(m => m.LoadDataAsync<MidiDevice, EightBitSaxLoungeDataRequest>(
            "GET", It.Is<EightBitSaxLoungeDataRequest>(r => r.RequestRoute == $"devices/{deviceName}"), "EightBitSaxLoungeDataLayer"))
            .ReturnsAsync(new List<MidiDevice> { device });

        _dataAccessMock.Setup(m => m.LoadDataAsync<Selector, EightBitSaxLoungeDataRequest>(
            "GET", It.Is<EightBitSaxLoungeDataRequest>(r => r.RequestRoute == $"selectors/{dependencyName}"), "EightBitSaxLoungeDataLayer"))
            .ReturnsAsync(new List<Selector> { selector });

        _dataAccessMock.Setup(m => m.LoadDataAsync<Effect, EightBitSaxLoungeDataRequest>(
            "GET", It.Is<EightBitSaxLoungeDataRequest>(r => r.RequestRoute == $"effects/{dependentEffectName}"), "EightBitSaxLoungeDataLayer"))
            .ReturnsAsync(new List<Effect> { dependentEffect });

        // Act
        var result = await _service.GetControlChangeMessageToSetDeviceEffectSettingAsync(deviceName, effectName, settingName, 127);

        // Assert
        Assert.Equal(15, result.Address);
        Assert.Equal(127, result.Value);
    }

    [Fact]
    public async Task UpdateDeviceByNameAsync_CallsSaveDataAsync()
    {
        // Arrange
        var deviceName = "TestDevice";
        var device = new MidiDevice { Name = deviceName, Description = "Desc", MidiConnectName = "Conn", MidiImplementation = [], DeviceEffects = [] };

        // Act
        await _service.UpdateDeviceByNameAsync(deviceName, device);

        // Assert
        _dataAccessMock.Verify(m => m.SaveDataAsync(
            "PUT",
            It.Is<EightBitSaxLoungeDataRequest>(r => r.RequestRoute == $"devices/{deviceName}" && r.RequestBody == device),
            "EightBitSaxLoungeDataLayer"), Times.Once);
    }

    [Fact]
    public async Task CreateEffectAsync_CallsSaveDataAsync()
    {
        // Arrange
        var effectName = "TestEffect";
        var effect = new Effect { Name = effectName, Description = "Desc" };

        // Act
        await _service.CreateEffectAsync(effectName, effect);

        // Assert
        _dataAccessMock.Verify(m => m.SaveDataAsync(
            "POST",
            It.Is<EightBitSaxLoungeDataRequest>(r => r.RequestRoute == "effects" && r.RequestBody == effect),
            "EightBitSaxLoungeDataLayer"), Times.Once);
    }

    [Fact]
    public async Task UpdateEffectByNameAsync_CallsSaveDataAsync()
    {
        // Arrange
        var effectName = "TestEffect";
        var effect = new Effect { Name = effectName, Description = "Desc" };

        // Act
        await _service.UpdateEffectByNameAsync(effectName, effect);

        // Assert
        _dataAccessMock.Verify(m => m.SaveDataAsync(
            "PUT",
            It.Is<EightBitSaxLoungeDataRequest>(r => r.RequestRoute == $"effects/{effectName}" && r.RequestBody == effect),
            "EightBitSaxLoungeDataLayer"), Times.Once);
    }

    [Fact]
    public async Task GetAllEffectsAsync_ReturnsAllEffects()
    {
        // Arrange
        var effects = new List<Effect>
        {
            new Effect { Name = "E1", Description = "D1" },
            new Effect { Name = "E2", Description = "D2" }
        };
        _dataAccessMock.Setup(m => m.LoadDataAsync<Effect, EightBitSaxLoungeDataRequest>(
            "GET", It.Is<EightBitSaxLoungeDataRequest>(r => r.RequestRoute == "effects/docs"), "EightBitSaxLoungeDataLayer"))
            .ReturnsAsync(effects);

        // Act
        var result = await _service.GetAllEffectsAsync();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Equal("E1", result[0].Name);
        Assert.Equal("E2", result[1].Name);
    }

    [Fact]
    public async Task CreateSelectorAsync_CallsSaveDataAsync()
    {
        // Arrange
        var selectorName = "TestSelector";
        var selector = new Selector { Name = selectorName, Selections = [] };

        // Act
        await _service.CreateSelectorAsync(selectorName, selector);

        // Assert
        _dataAccessMock.Verify(m => m.SaveDataAsync(
            "POST",
            It.Is<EightBitSaxLoungeDataRequest>(r => r.RequestRoute == "selectors" && r.RequestBody == selector),
            "EightBitSaxLoungeDataLayer"), Times.Once);
    }

    [Fact]
    public async Task UpdateSelectorByNameAsync_CallsSaveDataAsync()
    {
        // Arrange
        var selectorName = "TestSelector";
        var selector = new Selector { Name = selectorName, Selections = [] };

        // Act
        await _service.UpdateSelectorByNameAsync(selectorName, selector);

        // Assert
        _dataAccessMock.Verify(m => m.SaveDataAsync(
            "PUT",
            It.Is<EightBitSaxLoungeDataRequest>(r => r.RequestRoute == $"selectors/{selectorName}" && r.RequestBody == selector),
            "EightBitSaxLoungeDataLayer"), Times.Once);
    }

    [Fact]
    public async Task GetDeviceByNameAsync_ThrowsException_WhenMultipleFound()
    {
        // Arrange
        var deviceName = "DuplicateDevice";
        var devices = new List<MidiDevice>
        {
            new MidiDevice { Name = deviceName, Description = "D1", MidiConnectName = "C1", MidiImplementation = [], DeviceEffects = [] },
            new MidiDevice { Name = deviceName, Description = "D2", MidiConnectName = "C2", MidiImplementation = [], DeviceEffects = [] }
        };
        _dataAccessMock.Setup(m => m.LoadDataAsync<MidiDevice, EightBitSaxLoungeDataRequest>(
            "GET", It.Is<EightBitSaxLoungeDataRequest>(r => r.RequestRoute == $"devices/{deviceName}"), "EightBitSaxLoungeDataLayer"))
            .ReturnsAsync(devices);

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => _service.GetDeviceByNameAsync(deviceName));
    }
}
