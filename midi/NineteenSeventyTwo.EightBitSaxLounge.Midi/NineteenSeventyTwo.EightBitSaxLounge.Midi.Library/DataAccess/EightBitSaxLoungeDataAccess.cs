using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Net.Http;
using Microsoft.AspNetCore.Http;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.Library.DataAccess;

/// <summary>
/// The EightbitSaxLounge data layer has an api handling CRUD operations directly with its database.
/// </summary>
public class EightbitSaxLoungeDataAccess : IDataAccess
{
    private readonly IConfiguration _config;
    private readonly HttpClient _httpClient;
    private readonly IHttpContextAccessor? _httpContextAccessor;
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    // Allow injecting HttpClient and IHttpContextAccessor for tests. If null, create defaults.
    public EightbitSaxLoungeDataAccess(IConfiguration config, HttpClient? httpClient = null, IHttpContextAccessor? httpContextAccessor = null)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _httpClient = httpClient ?? new HttpClient();
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<List<T>> LoadDataAsync<T, TU>(string dataFunction, TU parameters, string connectionStringName)
    {
        if (string.IsNullOrWhiteSpace(dataFunction))
            throw new ArgumentException("dataFunction (HTTP verb) is required.", nameof(dataFunction));

        var baseUrl = _config.GetConnectionString(connectionStringName);
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException($"Connection string '{connectionStringName}' not found.");

        // Extract route and body from parameters (if present)
        TryGetPropertyValue(parameters, "RequestRoute", out var routeObj);
        TryGetPropertyValue(parameters, "RequestBody", out var bodyObj);

        var method = dataFunction.Trim().ToUpperInvariant();

        List<T>? result;
        string requestUri;

        if (method == "GET")
        {
            requestUri = BuildRequestUri(baseUrl, routeObj);

            using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            AddCorrelationIdHeader(request);
            using var response = await _httpClient.SendAsync(request).ConfigureAwait(false);
            result = await HandleResponse<T>(requestUri, response).ConfigureAwait(false);
        }
        else if (method == "POST")
        {
            requestUri = BuildRequestUri(baseUrl, routeObj);

            using var response = await SendBodyRequestAsync("POST", requestUri, bodyObj).ConfigureAwait(false);
            result = await HandleResponse<T>(requestUri, response).ConfigureAwait(false);
        }
        else
        {
            throw new ArgumentException($"Unsupported HTTP method: {dataFunction}", nameof(dataFunction));
        }

        if (result == null)
            throw new InvalidOperationException("No data returned from remote data layer.");

        return result;
    }

    public async Task SaveDataAsync<T>(string databaseFunction, T parameters, string connectionStringName)
    {
        // databaseFunction here is treated as an HTTP verb (POST/PUT)
        if (string.IsNullOrWhiteSpace(databaseFunction))
            throw new ArgumentException("databaseFunction (HTTP verb) is required.", nameof(databaseFunction));

        var baseUrl = _config.GetConnectionString(connectionStringName);
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException($"Connection string '{connectionStringName}' not found.");

        // parameters for SaveDataAsync are expected to carry RequestRoute and RequestBody
        TryGetPropertyValue(parameters, "RequestRoute", out var routeObj);
        TryGetPropertyValue(parameters, "RequestBody", out var bodyObj);

        var method = databaseFunction.Trim().ToUpperInvariant();
        string requestUri;
        bool success;

        if (method == "POST")
        {
            requestUri = BuildRequestUri(baseUrl, routeObj);

            using var response = await SendBodyRequestAsync("POST", requestUri, bodyObj).ConfigureAwait(false);
            success = response.IsSuccessStatusCode;
            if (!success)
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new HttpRequestException($"POST to {requestUri} failed ({(int)response.StatusCode}): {body}");
            }
        }
        else if (method == "PUT")
        {
            requestUri = BuildRequestUri(baseUrl, routeObj);

            using var response = await SendBodyRequestAsync("PUT", requestUri, bodyObj).ConfigureAwait(false);
            success = response.IsSuccessStatusCode;
            if (!success)
            {
                var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                throw new HttpRequestException($"PUT to {requestUri} failed ({(int)response.StatusCode}): {body}");
            }
        }
        else
        {
            throw new ArgumentException($"Unsupported HTTP method for save: {databaseFunction}", nameof(databaseFunction));
        }

        // single exit point
        if (!success)
            throw new InvalidOperationException("Save operation failed without an HTTP error.");
    }

    private async Task<List<T>> HandleResponse<T>(string requestUri, HttpResponseMessage response)
    {
        var responseBody = await response.Content.ReadAsStringAsync().ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"Request to {requestUri} failed ({(int)response.StatusCode}): {responseBody}");

        List<T>? result = null;

        // Try deserialize as List<T>
        try
        {
            var list = JsonSerializer.Deserialize<List<T>>(responseBody, _serializerOptions);
            if (list != null)
                result = list;
        }
        catch (JsonException) { }

        // Try deserialize single T and wrap
        if (result == null)
        {
            try
            {
                var single = JsonSerializer.Deserialize<T>(responseBody, _serializerOptions);
                if (single != null)
                    result = new List<T> { single };
            }
            catch (JsonException) { }
        }

        // If the API wraps the payload (e.g. { "data": [...] }) try to find first array or object property
        if (result == null)
        {
            try
            {
                using var doc = JsonDocument.Parse(responseBody);
                foreach (var prop in doc.RootElement.EnumerateObject())
                {
                    if (result != null) break;

                    if (prop.Value.ValueKind == JsonValueKind.Array)
                    {
                        var arrJson = prop.Value.GetRawText();
                        var list = JsonSerializer.Deserialize<List<T>>(arrJson, _serializerOptions);
                        if (list != null)
                        {
                            result = list;
                            break;
                        }
                    }

                    if (prop.Value.ValueKind == JsonValueKind.Object)
                    {
                        var objJson = prop.Value.GetRawText();
                        var single = JsonSerializer.Deserialize<T>(objJson, _serializerOptions);
                        if (single != null)
                        {
                            result = new List<T> { single };
                            break;
                        }
                    }
                }
            }
            catch (JsonException) { }
        }

        if (result == null)
            throw new InvalidOperationException("Unable to deserialize response into List/ single " + typeof(T).Name + ".");

        return result;
    }

    private static void TryGetPropertyValue<TU>(TU parameters, string propertyName, out object? value)
    {
        value = null;
        bool found = false;

        if (parameters == null)
        {
            // single exit point: found remains false
        }
        else
        {
            // If TU is a dictionary-like object, try to handle common cases
            if (!found && parameters is System.Collections.IDictionary dict)
            {
                if (dict.Contains(propertyName))
                {
                    value = dict[propertyName];
                    found = true;
                }

                // case-insensitive search
                if (!found)
                {
                    foreach (var key in dict.Keys)
                    {
                        if (key?.ToString()?.Equals(propertyName, StringComparison.OrdinalIgnoreCase) == true)
                        {
                            value = dict[key];
                            found = true;
                            break;
                        }
                    }
                }
            }

            var type = parameters.GetType();

            if (!found)
            {
                var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (prop != null)
                {
                    value = prop.GetValue(parameters);
                    found = true;
                }
            }

            // If it's a simple string and propertyName requested is "route", treat the string as the route
            if (!found && parameters is string s && propertyName.Equals("route", StringComparison.OrdinalIgnoreCase))
            {
                value = s;
            }
        }
    }

    // Helper to send POST/PUT with optional JSON body and return the response.
    private async Task<HttpResponseMessage> SendBodyRequestAsync(string method, string requestUri, object? body)
    {
        HttpMethod httpMethod = method.Trim().ToUpperInvariant() switch
        {
            "POST" => HttpMethod.Post,
            "PUT" => HttpMethod.Put,
            _ => throw new ArgumentException($"Unsupported body request method: {method}", nameof(method))
        };

        using var message = new HttpRequestMessage(httpMethod, requestUri);
        
        // Add correlation ID header from current HTTP context
        AddCorrelationIdHeader(message);
        
        if (body != null)
        {
            var json = JsonSerializer.Serialize(body, _serializerOptions);
            message.Content = new StringContent(json, Encoding.UTF8, "application/json");
        }

        var response = await _httpClient.SendAsync(message).ConfigureAwait(false);
        return response;
    }

    // Helper to build request URI from baseUrl and a route object (handles null/empty)
    private static string BuildRequestUri(string baseUrl, object? routeObj)
    {
        var route = routeObj?.ToString() ?? string.Empty;
        return $"{baseUrl.TrimEnd('/')}/{route.TrimStart('/')}";
    }

    // Helper to add correlation ID header to outgoing requests
    private void AddCorrelationIdHeader(HttpRequestMessage request)
    {
        // Get correlation ID from HttpContext.Items (set by CorrelationIdMiddleware)
        var correlationId = _httpContextAccessor?.HttpContext?.Items["CorrelationId"] as string;
        if (!string.IsNullOrEmpty(correlationId))
        {
            request.Headers.TryAddWithoutValidation("X-Correlation-ID", correlationId);
        }
    }
}
