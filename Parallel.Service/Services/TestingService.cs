// Copyright 2026 Kyle Ebbinga

using Microsoft.AspNetCore.SignalR;
using Parallel.Core.Utils;

namespace Parallel.Service.Services
{
    public class TestingService : BackgroundService
    {
        private readonly ILogger<TestingService> _logger;
        private readonly IHubContext<MessageHub> _hub;
        private readonly ProcessMonitor _monitor;

        public TestingService(ILogger<TestingService> logger, IHubContext<MessageHub> hub, ProcessMonitor monitor)
        {
            _logger = logger;
            _hub = hub;
            _monitor = monitor;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _monitor.Refresh();
                long bytes = Random.Shared.NextInt64(0, 1024 * 1024 * 1024);
                await _hub.Clients.All.SendAsync("StorageSizeUpdate", Formatter.FromBytes(bytes), stoppingToken);
                await _hub.Clients.All.SendAsync("LastSyncUpdate", Formatter.FromDateTime(DateTime.UtcNow), stoppingToken);
                await Task.Delay(1500, stoppingToken);
            }
        }
    }
}