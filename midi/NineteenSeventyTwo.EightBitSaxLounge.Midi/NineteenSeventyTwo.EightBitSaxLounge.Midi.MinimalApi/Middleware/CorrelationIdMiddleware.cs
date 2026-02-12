using Microsoft.AspNetCore.Http;
using System;
using System.Threading.Tasks;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Middleware
{
    /// <summary>
    /// Middleware to handle correlation IDs for request tracking across services.
    /// </summary>
    public class CorrelationIdMiddleware
    {
        private const string CorrelationIdHeader = "X-Correlation-ID";
        private readonly RequestDelegate _next;
        private readonly ILogger<CorrelationIdMiddleware> _logger;

        public CorrelationIdMiddleware(RequestDelegate next, ILogger<CorrelationIdMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Get or generate correlation ID
            string correlationId = context.Request.Headers[CorrelationIdHeader].FirstOrDefault()
                                 ?? Guid.NewGuid().ToString();

            // Store in HttpContext for access by handlers
            context.Items["CorrelationId"] = correlationId;

            // Add to response headers
            context.Response.OnStarting(() =>
            {
                if (!context.Response.Headers.ContainsKey(CorrelationIdHeader))
                {
                    context.Response.Headers.Append(CorrelationIdHeader, correlationId);
                }
                return Task.CompletedTask;
            });

            using (_logger.BeginScope(new Dictionary<string, object>
            {
                ["CorrelationId"] = correlationId
            }))
            {
                // Log request with correlation ID
                _logger.LogInformation("Request started: {Method} {Path}",
                    context.Request.Method, context.Request.Path);

                await _next(context);

                // Log response with correlation ID
                _logger.LogInformation("Request completed: {Method} {Path} {StatusCode}",
                    context.Request.Method, context.Request.Path, context.Response.StatusCode);
            }
        }
    }

    /// <summary>
    /// Extension methods for HttpContext to access correlation ID.
    /// </summary>
    public static class CorrelationIdExtensions
    {
        public static string? GetCorrelationId(this HttpContext context)
        {
            return context.Items["CorrelationId"] as string;
        }
    }
}
