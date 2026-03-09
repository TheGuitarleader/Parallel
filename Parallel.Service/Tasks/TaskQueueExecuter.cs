// Copyright 2026 Kyle Ebbinga

namespace Parallel.Service.Tasks
{
    public class TaskQueueExecutor : BackgroundService
    {
        private readonly ILogger<TaskQueueExecutor> _logger;
        private readonly TaskQueuer _queuer;

        public TaskQueueExecutor(ILogger<TaskQueueExecutor> logger, TaskQueuer queuer)
        {
            _logger = logger;
            _queuer = queuer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                Func<Task>? task = null;

                try
                {
                    task = await _queuer.WaitAsync(stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    // graceful shutdown
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed waiting for queued task");
                    continue;
                }

                try
                {
                    await task();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Queued task failed");
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