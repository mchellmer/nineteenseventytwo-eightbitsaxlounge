using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.UnitTests;

public abstract class TestBase
{
    protected static async Task<(int StatusCode, string Body)> ExecuteResultAsync(IResult result)
    {
        // Try to extract common properties without executing the result (avoids needing RequestServices)
        var type = result.GetType();

        // Status code property (common on many Result types)
        var statusProp = type.GetProperty("StatusCode");
        int status = statusProp != null && statusProp.GetValue(result) is int s ? s : 200;

        // Common value/payload property names used by different IResult implementations
        var valueProp = type.GetProperty("Value")
                     ?? type.GetProperty("Payload")
                     ?? type.GetProperty("Result")
                     ?? type.GetProperty("ValueAsObject")
                     ?? type.GetProperty("Content");

        if (valueProp != null)
        {
            var value = valueProp.GetValue(result);
            if (value == null)
                return (status, string.Empty);

            if (value is string str)
                return (status, str);

            try
            {
                var body = JsonSerializer.Serialize(value);
                return (status, body);
            }
            catch
            {
                return (status, value.ToString() ?? string.Empty);
            }
        }

        // Fallback: execute the result but ensure RequestServices is not null to prevent ArgumentNullException
        var ctx = new DefaultHttpContext();
        ctx.RequestServices = new ServiceCollection()
            .AddLogging() // provide minimal services commonly required by result execution
            .BuildServiceProvider();
        ctx.Response.Body = new MemoryStream();

        await result.ExecuteAsync(ctx).ConfigureAwait(false);

        ctx.Response.Body.Seek(0, SeekOrigin.Begin);
        using var sr = new StreamReader(ctx.Response.Body);
        var bodyText = await sr.ReadToEndAsync().ConfigureAwait(false);
        return (ctx.Response.StatusCode, bodyText);
    }
}
