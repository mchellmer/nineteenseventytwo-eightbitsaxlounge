namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Services;

/// <summary>
/// Service for proxying device control requests to the Windows PC MIDI service
/// </summary>
public class MidiDeviceProxyService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MidiDeviceProxyService> _logger;
    private readonly string _deviceServiceUrl;

    public MidiDeviceProxyService(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<MidiDeviceProxyService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _deviceServiceUrl = configuration["MidiDeviceService:Url"] 
            ?? throw new InvalidOperationException("MidiDeviceService:Url not configured");
        
        _logger.LogInformation("MIDI Device Proxy configured for: {Url}", _deviceServiceUrl);
    }

    /// <summary>
    /// Proxy a control change message to the Windows PC device service
    /// </summary>
    public async Task<IResult> ProxyControlChangeMessage(
        string deviceMidiConnectName,
        string address,
        string value,
        string? authToken)
    {
        try
        {
            var requestUrl = $"{_deviceServiceUrl}/api/Midi/SendControlChangeMessage";
            var request = new HttpRequestMessage(HttpMethod.Post, requestUrl);
            
            // Forward authorization if present
            if (!string.IsNullOrWhiteSpace(authToken))
            {
                request.Headers.Add("Authorization", authToken);
            }

            // Build request body
            var payload = new
            {
                DeviceMidiConnectName = deviceMidiConnectName,
                Address = address,
                Value = value
            };

            request.Content = JsonContent.Create(payload);

            _logger.LogInformation(
                "Proxying device request to {Url}: Device={Device}, Address={Address}, Value={Value}",
                requestUrl, deviceMidiConnectName, address, value);

            var response = await _httpClient.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Device request successful");
                return Results.Ok(new { success = true, message = "Device updated" });
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "Device request failed: {StatusCode} - {Error}",
                response.StatusCode, errorContent);

            return Results.Problem(
                statusCode: (int)response.StatusCode,
                detail: $"Device service error: {errorContent}");
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "Failed to connect to device service at {Url}", _deviceServiceUrl);
            return Results.Problem(
                statusCode: 503,
                detail: "Device service unavailable. Check PC service is running.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying device request");
            return Results.Problem(
                statusCode: 500,
                detail: "Internal error while contacting device service");
        }
    }
}
