using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models;
using NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Models.Winmm;
using System.Net.Http.Json;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.Midi;

public class WinmmMidiDeviceService : IMidiDeviceService, IMidiProxyService
{
    private readonly ILogger<WinmmMidiDeviceService> _logger;
    private readonly IMidiOutDeviceFactory _deviceFactory;
    private readonly HttpClient? _httpClient;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly string? _proxyUrl;
    private readonly string? _bypassKey;
    public bool IsProxyEnabled { get; }
    public string? ProxyUrl => _proxyUrl;

    private const string ProxyHeaderName = "X-Proxied-Request";
    private const string BypassKeyHeaderName = "X-Bypass-Key";

    public WinmmMidiDeviceService(
        ILogger<WinmmMidiDeviceService> logger,
        IMidiOutDeviceFactory deviceFactory,
        IConfiguration configuration,
        HttpClient? httpClient = null,
        IHttpContextAccessor? httpContextAccessor = null)
    {
        _logger = logger;
        _deviceFactory = deviceFactory;
        _httpClient = httpClient;
        _httpContextAccessor = httpContextAccessor;

        var configuredUrl = configuration?["MidiDeviceService:Url"];
        _bypassKey = configuration?["MidiDeviceService:BypassKey"];

        if (!string.IsNullOrWhiteSpace(configuredUrl))
        {
            _proxyUrl = configuredUrl.TrimEnd('/');
            IsProxyEnabled = true;
            _logger.LogInformation("MIDI Device Service configured to proxy to: {Url}", _proxyUrl);

            if (!string.IsNullOrWhiteSpace(_bypassKey))
            {
                _logger.LogInformation("Bypass authentication key configured for proxy requests");
            }
        }
        else
        {
            _proxyUrl = null;
            IsProxyEnabled = false;
            _logger.LogInformation("MIDI Device Service configured for local device access only");
        }
    }

    public async Task SendControlChangeMessageByDeviceMidiConnectNameAsync(string midiConnectName, ControlChangeMessage controlChangeMessage)
    {
        _logger.LogInformation("Sending control change message to device {DeviceName}: Address={Address}, Value={Value}",
            midiConnectName, controlChangeMessage.Address, controlChangeMessage.Value);

        // Check if this request is already a proxied request to prevent infinite recursion
        var isAlreadyProxied = _httpContextAccessor?.HttpContext?.Request.Headers.ContainsKey(ProxyHeaderName) ?? false;

        if (IsProxyEnabled && _httpClient != null && !isAlreadyProxied)
        {
            await SendViaProxyAsync(midiConnectName, controlChangeMessage);
        }
        else
        {
            await SendViaLocalDeviceAsync(midiConnectName, controlChangeMessage);
        }
    }

    private async Task SendViaProxyAsync(string midiConnectName, ControlChangeMessage controlChangeMessage)
    {
        try
        {
            var requestUrl = $"{_proxyUrl}/api/Midi/SendControlChangeMessage";
            var payload = new
            {
                DeviceMidiConnectName = midiConnectName,
                Address = controlChangeMessage.Address,
                Value = controlChangeMessage.Value
            };

            _logger.LogDebug(
                "Proxying device request to {Url}: Device={Device}, Address={Address}, Value={Value}",
                requestUrl, midiConnectName, controlChangeMessage.Address, controlChangeMessage.Value);

            using var request = new HttpRequestMessage(HttpMethod.Post, requestUrl)
            {
                Content = JsonContent.Create(payload)
            };

            // Add header to prevent infinite recursion
            request.Headers.Add(ProxyHeaderName, "true");

            // Add bypass key if configured
            if (!string.IsNullOrWhiteSpace(_bypassKey))
            {
                request.Headers.Add(BypassKeyHeaderName, _bypassKey);
            }

            var response = await _httpClient!.SendAsync(request);
            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Control change message proxied to device {DeviceName}", midiConnectName);
                return;
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "Proxy request failed: {StatusCode} - {Error}",
                response.StatusCode, errorContent);
            throw new Exception($"Remote device service error {(int)response.StatusCode}: {errorContent}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to remote device service at {Url}", _proxyUrl);
            throw new Exception($"Remote device service unavailable at {_proxyUrl}: {ex.Message}", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying control change message to device {DeviceName}", midiConnectName);
            throw;
        }
    }


    private async Task SendViaLocalDeviceAsync(string midiConnectName, ControlChangeMessage controlChangeMessage)
    {
        try
        {
            _logger.LogInformation("Retrieving from available MIDI output devices");
            using var device = _deviceFactory.Create(midiConnectName);
            var (opened, openResult, openError) = device.TryOpen();
            if (!opened)
            {
                _logger.LogError("Failed to open device {DeviceName}: {Error} (MMResult: {Result})", midiConnectName, openError, openResult);
                throw new Exception($"Failed to open device {midiConnectName}: {openError} (MMResult: {openResult})");
            }
            (var success, var mmResult, var error) =
                await device.TrySendControlChangeMessageDetailedAsync(controlChangeMessage.Address, controlChangeMessage.Value, closeAfterSend: true).ConfigureAwait(false);
            if (!success)
            {
                _logger.LogError(
                    "Error sending control change message to device {DeviceName}: {Error} (MMResult: {MmResult})",
                    midiConnectName, error, mmResult);
                throw new Exception(
                    $"Error sending control change message to device {midiConnectName}: {error} (MMResult: {mmResult})");
            }
            _logger.LogInformation("Control change message sent to device {DeviceName}", midiConnectName);
        }
        catch (Exception e)
        {
            _logger.LogError(
                e,
                "Error sending control change message to device {DeviceName}: {Message}", midiConnectName, e.Message);
            throw new Exception($"Error sending control change message to device {midiConnectName}: {e.Message}");
        }
    }
}

