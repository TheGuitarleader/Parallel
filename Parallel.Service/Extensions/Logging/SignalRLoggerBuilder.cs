// Copyright 2026 Kyle Ebbinga

namespace Parallel.Service.Extensions.Logging
{
    public static class SignalRLoggerBuilder
    {
        /// <summary>
        /// Adds the current <see cref="T:Microsoft.Extensions.Logging.ILoggingBuilder"/> to the assembly SignalR logger instance.
        /// </summary>
        /// <param name="builder">The <see cref="T:Microsoft.Extensions.Logging.ILoggingBuilder" /> to add logging provider to.</param>
        /// <returns>Reference to the supplied <paramref name="builder"/>.</returns>
        public static ILoggingBuilder AddSignalR(this ILoggingBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            builder.Services.AddSingleton<ILoggerProvider, SignalRLoggerProvider>();
            builder.AddFilter<SignalRLoggerProvider>(null, LogLevel.Trace);
            return builder;
        }
    }
}