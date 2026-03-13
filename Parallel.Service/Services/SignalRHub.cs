// Copyright 2026 Kyle Ebbinga

using Microsoft.AspNetCore.SignalR;

namespace Parallel.Service.Services
{
    public class SignalRHub : Hub
    {
        private readonly ILogger<SignalRHub> _logger;

        public SignalRHub(ILogger<SignalRHub> logger)
        {
            _logger = logger;
        }

        public override Task OnDisconnectedAsync(Exception? exception)
        {
            if (exception != null) _logger.LogError(exception, "SignalR threw exception");
            return base.OnDisconnectedAsync(exception);
        }
    }
}