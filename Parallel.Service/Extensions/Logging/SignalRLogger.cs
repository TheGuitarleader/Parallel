// Copyright 2026 Kyle Ebbinga

using Microsoft.AspNetCore.SignalR;
using Parallel.Service.Services;

namespace Parallel.Service.Extensions.Logging
{
    /// <summary>
    /// Represents a SignalR logger that sends log messages to GUIs.
    /// </summary>
    public class SignalRLogger : ILogger
    {
        private readonly string _categoryName;
        private readonly IExternalScopeProvider _scopeProvider;
        private readonly IServiceProvider _services;
        private IHubContext<MessageHub>? _hub;

        private IHubContext<MessageHub>? Hub => _hub ??= _services.GetService<IHubContext<MessageHub>>();

        public SignalRLogger(string categoryName, IExternalScopeProvider scopeProvider, IServiceProvider services)
        {
            _categoryName = categoryName;
            _scopeProvider = scopeProvider;
            _services = services;
        }

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Hub?.Clients.All.SendAsync("LogMessage", $"LogLevel: {logLevel}, EventId: {eventId}, State: {state}", exception);

            /*switch(logLevel)
            {
                case LogLevel.Debug:
                    _hub.Clients.All.SendAsync()
                    break;

                case LogLevel.Information:
                    _entex?.Log(formatter(state, exception), "INFO", ConsoleColor.Gray);
                    break;

                case LogLevel.Warning:
                    _entex?.Log(formatter(state, exception), "WARN", ConsoleColor.Yellow);
                    break;

                case LogLevel.Error:
                    _entex?.Log(formatter(state, exception), "ERROR", ConsoleColor.Red);
                    break;

                case LogLevel.Critical:
                    _entex?.Log(formatter(state, exception), "FATAL", ConsoleColor.DarkRed);
                    break;
            }*/
        }

        /// <inheritdoc/>
        public bool IsEnabled(LogLevel logLevel)
        {
            return logLevel > 0;
        }

        /// <inheritdoc/>
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        {
            return _scopeProvider.Push(state);
        }
    }
}