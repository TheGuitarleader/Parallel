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
            long bytes = 0;
            while (!stoppingToken.IsCancellationRequested)
            {
                _monitor.Refresh();
                await _hub.Clients.All.SendAsync("TotalStorageUpdate", Formatter.FromBytes(bytes += 500), stoppingToken);
                await _hub.Clients.All.SendAsync("LastSyncUpdate", Formatter.FromDateTime(DateTime.UtcNow), stoppingToken);
                await Task.Delay(1500, stoppingToken);
            }
        }
    }
}