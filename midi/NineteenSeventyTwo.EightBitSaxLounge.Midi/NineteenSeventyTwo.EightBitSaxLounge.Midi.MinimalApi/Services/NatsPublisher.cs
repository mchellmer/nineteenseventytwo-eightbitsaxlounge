using System.Text.Json;
using NATS.Client.Core;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Services;

public class NatsPublisher : INatsPublisher, IAsyncDisposable
{
    private readonly NatsConnection _nc;
    private readonly ILogger<NatsPublisher> _logger;

    public NatsPublisher(IConfiguration configuration, ILogger<NatsPublisher> logger)
    {
        _logger = logger;

        var url = configuration["Nats:Url"] ?? "nats://nats:4222";
        var user = configuration["Nats:User"];
        var pass = configuration["Nats:Pass"];

        var authOpts = (!string.IsNullOrEmpty(user) && !string.IsNullOrEmpty(pass))
            ? new NatsAuthOpts { Username = user, Password = pass }
            : NatsAuthOpts.Default;

        _nc = new NatsConnection(new NatsOpts { Url = url, AuthOpts = authOpts });
        _logger.LogInformation("NatsPublisher initialised: url={Url} user={User}", url, user ?? "(none)");
    }

    public async Task PublishAsync(string subject, string value)
    {
        try
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(new { value });
            await _nc.PublishAsync(subject, payload);
            _logger.LogInformation("Published NATS event {Subject} = {Value}", subject, value);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish NATS event {Subject}", subject);
        }
    }

    public async ValueTask DisposeAsync() => await _nc.DisposeAsync();
}
