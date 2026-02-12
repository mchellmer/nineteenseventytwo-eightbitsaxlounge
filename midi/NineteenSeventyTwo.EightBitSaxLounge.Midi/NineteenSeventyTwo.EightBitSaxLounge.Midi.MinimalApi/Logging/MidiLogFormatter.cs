using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging.Console;
using System;
using System.Collections.Generic;
using System.IO;

namespace NineteenSeventyTwo.EightBitSaxLounge.Midi.MinimalApi.Logging;

/// <summary>
/// Custom log formatter that adds [midi] prefix to all log messages.
/// </summary>
public class MidiLogFormatter : ConsoleFormatter
{
    public MidiLogFormatter() : base("midi") { }

    public override void Write<TState>(
        in LogEntry<TState> logEntry,
        IExternalScopeProvider? scopeProvider,
        TextWriter textWriter)
    {
        string message = logEntry.Formatter?.Invoke(logEntry.State, logEntry.Exception) ?? string.Empty;
            
        if (string.IsNullOrEmpty(message))
        {
            return;
        }

        // Extract CorrelationId from scope if available
        string? correlationId = null;
        scopeProvider?.ForEachScope((scope, state) =>
        {
            if (scope is IEnumerable<KeyValuePair<string, object>> scopeItems)
            {
                foreach (var item in scopeItems)
                {
                    if (item.Key == "CorrelationId")
                    {
                        correlationId = item.Value?.ToString();
                    }
                }
            }
        }, (object?)null);

        // Add [midi] prefix and log level
        textWriter.Write($"{DateTimeOffset.Now:yyyy-MM-dd HH:mm:ss.fff} ");
        textWriter.Write($"[{logEntry.LogLevel}] ");
        textWriter.Write("[midi] ");
        
        textWriter.Write(message);

        if (logEntry.Exception != null)
        {
            textWriter.Write($" Exception: {logEntry.Exception}");
        }
        
        // Add correlation ID at the end if available
        if (!string.IsNullOrEmpty(correlationId))
        {
            textWriter.Write($" correlationID={correlationId}");
        }

        textWriter.WriteLine();
    }
}

/// <summary>
/// Extension methods for configuring MIDI logging.
/// </summary>
public static class MidiLoggingExtensions
{
    /// <summary>
    /// Configure logging with [midi] prefix for all messages.
    /// </summary>
    public static ILoggingBuilder ConfigureMidiLogging(this ILoggingBuilder builder)
    {
        builder.ClearProviders();
        builder.AddConsole(options =>
        {
            options.FormatterName = "midi";
        });
        builder.AddConsoleFormatter<MidiLogFormatter, Microsoft.Extensions.Logging.Console.ConsoleFormatterOptions>();
            
        return builder;
    }
}