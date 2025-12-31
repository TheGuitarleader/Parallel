// Copyright 2025 Kyle Ebbinga

using System.Collections.Concurrent;
using Serilog.Core;
using Serilog.Events;

namespace Parallel.Core.Utils
{
    /// <summary>
    /// Represents an event tracking sink for Serilog.
    /// </summary>
    public class LogEventTracker : ILogEventSink
    {
        /// <summary>
        /// Gets an array of warning log messages.
        /// </summary>
        public readonly ConcurrentBag<string> Warnings = new ConcurrentBag<string>();

        /// <summary>
        /// Gets an array of error log messages.
        /// </summary>
        public readonly ConcurrentBag<string> Errors = new ConcurrentBag<string>();

        /// <inheritdoc />
        public void Emit(LogEvent logEvent)
        {
            if (logEvent.Level == LogEventLevel.Warning)
            {
                Warnings.Add($"[{logEvent.Timestamp.LocalDateTime:HH:mm:ss.fff}]: {logEvent.RenderMessage()}");
            }

            if (logEvent.Level >= LogEventLevel.Error)
            {
                Errors.Add(logEvent.Exception != null ? $"[{logEvent.Timestamp.LocalDateTime:HH:mm:ss.fff}]: {logEvent.Exception}" : $"[{logEvent.Timestamp.LocalDateTime:HH:mm:ss.fff}]: {logEvent.RenderMessage()}");
            }
        }
    }
}