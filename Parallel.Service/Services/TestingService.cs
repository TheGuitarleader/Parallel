// Copyright 2026 Kyle Ebbinga

using System.Diagnostics;
using Microsoft.AspNetCore.SignalR;
using Parallel.Core.Utils;
using Parallel.Service.Tasks;

namespace Parallel.Service.Services
{
    public class TestingService : BackgroundService
    {
        private readonly ILogger<TestingService> _logger;
        private readonly IHubContext<MessageHub> _hub;
        private readonly ProcessMonitor _monitor;
        private readonly TaskQueuer _queuer;
        private readonly Stopwatch _sw;

        public TestingService(ILogger<TestingService> logger, IHubContext<MessageHub> hub, ProcessMonitor monitor, TaskQueuer queuer)
        {
            _logger = logger;
            _hub = hub;
            _monitor = monitor;
            _queuer = queuer;
            _sw = Stopwatch.StartNew();
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Task task1 = LoopTask1Async(stoppingToken);
            Task task2 = LoopTask2Async(stoppingToken);
            Task task3 = LoopTask3Async(stoppingToken);
            await Task.WhenAll(task1, task2, task3);
        }

        private async Task LoopTask1Async(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _queuer.Enqueue("loop1", async () =>
                {
                    _logger.LogInformation($"loop1 @ {_sw.Elapsed}");
                    await Task.Delay(1000, stoppingToken);
                });
                
                await Task.Delay(800, stoppingToken);
            }
        }
        
        private async Task LoopTask2Async(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _queuer.Enqueue("loop2", async () =>
                {
                    _logger.LogInformation($"loop2 @ {_sw.Elapsed}");
                    await Task.Delay(2000, stoppingToken);
                });
                
                await Task.Delay(1000, stoppingToken);
            }
        }
        
        private async Task LoopTask3Async(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _queuer.Enqueue("loop3", async () =>
                {
                    _logger.LogInformation($"loop3 @ {_sw.Elapsed}");
                    await Task.Delay(3000, stoppingToken);
                });
                
                await Task.Delay(100, stoppingToken);
            }
        }
    }
}