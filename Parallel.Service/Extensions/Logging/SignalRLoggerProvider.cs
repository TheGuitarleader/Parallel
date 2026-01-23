// Copyright 2026 Kyle Ebbinga

using Microsoft.AspNetCore.SignalR;
using Parallel.Service.Services;

namespace Parallel.Service.Extensions.Logging
{
    [ProviderAlias("SignalRLogger")]
    public class SignalRLoggerProvider : ILoggerProvider, ISupportExternalScope
    {
        private IExternalScopeProvider _scopeProvider = default!;
        private readonly IServiceProvider _services;

        public SignalRLoggerProvider(IServiceProvider services)
        {
            _services = services;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return new SignalRLogger(
                categoryName,
                _scopeProvider,
                _services
            );
        }

        public void SetScopeProvider(IExternalScopeProvider scopeProvider)
        {
            _scopeProvider = scopeProvider;
        }

        public void Dispose() { }
    }
}