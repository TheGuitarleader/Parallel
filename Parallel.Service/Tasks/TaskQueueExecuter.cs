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
                    _logger.LogError(ex, $"Task execution failed!");
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