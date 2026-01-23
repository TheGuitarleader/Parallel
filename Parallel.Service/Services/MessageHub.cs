// Copyright 2026 Kyle Ebbinga

using Grpc.Core;
using Microsoft.AspNetCore.SignalR;
using Parallel.Service;

namespace Parallel.Service.Services
{
    public class MessageHub : Hub
    {
        private readonly ILogger<MessageHub> _logger;

        public MessageHub(ILogger<MessageHub> logger)
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