using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Models;
using System.Text.Json;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Handlers;

public class UploadDeviceHandler : IEndpointHandler<string, IResult>
{
    private readonly ILogger<UploadDeviceHandler> _logger;
    private readonly IMidiDataService _midiDataService;

    public UploadDeviceHandler(
        ILogger<UploadDeviceHandler> logger,
        IMidiDataService midiDataService)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _midiDataService = midiDataService ?? throw new ArgumentNullException(nameof(midiDataService));
    }

    public async Task<IResult> HandleAsync(string deviceName)
    {
        _logger.LogInformation("Uploading device {DeviceName} configuration to databases", deviceName);

        try
        {
            var filePath = $"appsettings.Devices.{deviceName}.json";
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("Device configuration file {FilePath} not found", filePath);
                return Results.NotFound(new { Message = $"Configuration file for device '{deviceName}' not found." });
            }

            var jsonContent = await File.ReadAllTextAsync(filePath);
            var deviceUploadWrapper = JsonSerializer.Deserialize<DeviceUploadWrapper>(
                jsonContent,
                new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

            if (deviceUploadWrapper == null)
            {
                _logger.LogError("Failed to deserialize device configuration for {DeviceName}", deviceName);
                return Results.Problem("Failed to deserialize device configuration.");
            }

            // 1. Populate 'devices' database
            if (deviceUploadWrapper.Devices != null)
            {
                foreach (var device in deviceUploadWrapper.Devices)
                {
                    var existingDevice = await _midiDataService.GetDeviceByNameAsync(device.Name);
                    if (existingDevice != null)
                    {
                        await _midiDataService.UpdateDeviceByNameAsync(device.Name, device);
                        _logger.LogInformation("Updated existing device {DeviceName} in 'devices' database", device.Name);
                    }
                    else
                    {
                        await _midiDataService.CreateDeviceAsync(device);
                        _logger.LogInformation("Created new device {DeviceName} in 'devices' database", device.Name);
                    }
                }
            }

            // 2. Populate 'selectors' database
            if (deviceUploadWrapper.Selectors != null)
            {
                foreach (var selector in deviceUploadWrapper.Selectors)
                {
                    var existingSelector = await _midiDataService.GetSelectorByNameAsync(selector.Name);
                    if (existingSelector != null)
                    {
                        await _midiDataService.UpdateSelectorByNameAsync(selector.Name, selector);
                        _logger.LogInformation("Updated selector {SelectorName} in 'selectors' database", selector.Name);
                    }
                    else
                    {
                        await _midiDataService.CreateSelectorAsync(selector.Name, selector);
                        _logger.LogInformation("Created selector {SelectorName} in 'selectors' database", selector.Name);
                    }
                }
            }

            // 3. Populate 'effects' database
            if (deviceUploadWrapper.Effects != null)
            {
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                foreach (var effectElement in deviceUploadWrapper.Effects)
                {
                    if (effectElement.TryGetProperty("Name", out var nameElement))
                    {
                        var effectName = nameElement.GetString();
                        if (string.IsNullOrEmpty(effectName)) continue;

                        var existingEffect = await _midiDataService.GetEffectByNameAsync(effectName);
                        if (existingEffect != null)
                        {
                            // Merge DeviceSettings if they exist in JSON
                            if (effectElement.TryGetProperty("DeviceSettings", out var settingsElement))
                            {
                                var deviceSettings = JsonSerializer.Deserialize<List<DeviceSetting>>(settingsElement.GetRawText(), jsonOptions);
                                if (deviceSettings != null)
                                {
                                    existingEffect.DeviceSettings ??= new List<DeviceSetting>();
                                    foreach (var setting in deviceSettings)
                                    {
                                        var existingSettingIndex = existingEffect.DeviceSettings.FindIndex(ds => ds.Name == setting.Name && ds.DeviceName == setting.DeviceName);
                                        if (existingSettingIndex >= 0)
                                        {
                                            existingEffect.DeviceSettings[existingSettingIndex] = setting;
                                        }
                                        else
                                        {
                                            existingEffect.DeviceSettings.Add(setting);
                                        }
                                    }
                                }
                            }
                            
                            // Update Description if provided in JSON
                            if (effectElement.TryGetProperty("Description", out var descElement))
                            {
                                existingEffect.Description = descElement.GetString() ?? existingEffect.Description;
                            }

                            await _midiDataService.UpdateEffectByNameAsync(effectName, existingEffect);
                            _logger.LogInformation("Updated existing effect {EffectName} in 'effects' database", effectName);
                        }
                        else
                        {
                            _logger.LogWarning("Effect {EffectName} not found, skipping device settings upload", effectName);
                        }
                    }
                }
            }

            return Results.Ok(new { Message = $"Device '{deviceName}' configuration uploaded successfully." });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload device {DeviceName}", deviceName);
            return Results.Problem(
                detail: ex.Message,
                title: "Device upload failed",
                statusCode: 500);
        }
    }
}
