// Copyright 2026 Kyle Ebbinga

using Parallel.Service.Tasks;
using Serilog;

namespace Parallel.Service.Services
{
    public class TaskQueueExecuter : BackgroundService
    {
        private readonly ILogger<TaskQueueExecuter> _logger;
        private readonly TaskQueuer _queuer;

        public TaskQueueExecuter(ILogger<TaskQueueExecuter> logger, TaskQueuer queuer)
        {
            _logger = logger;
            _queuer = queuer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    Func<Task> task = await _queuer.WaitAsync(stoppingToken);
                    await task();
                }
                catch (OperationCanceledException)
                {
                    // graceful shutdown
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Task execution failed: '{ex.Source}' (Reason: {ex.Message})");
                }
            }
        }

        public override Task StopAsync(CancellationToken cancellationToken)
        {
            if (!_queuer.IsEmpty) _logger.LogInformation($"Shutdown requested. Cancelling {_queuer.Count:N0} tasks...");
            return base.StopAsync(cancellationToken);
        }
    }
}