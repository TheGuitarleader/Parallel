// Copyright 2026 Entex Interactive, LLC

using Parallel.Service.Tasks;
using Serilog;

namespace Parallel.Service.Services
{
    public class TaskWorkerService : BackgroundService
    {
        private readonly ILogger<TaskWorkerService> _logger;
        private readonly TaskQueuer _queuer;

        public TaskWorkerService(ILogger<TaskWorkerService> logger, TaskQueuer queuer)
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
                    Func<CancellationToken, Task> task = await _queuer.WaitAsync(stoppingToken);
                    await task(stoppingToken);
                }
                catch (OperationCanceledException ex)
                {
                    _logger.LogWarning($"Task cancelled (Reason: {ex.Message})");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Task execution failed: '{ex.Source}' (Reason: {ex.Message})");
                }
                finally
                {
                    _logger.LogDebug($"Remaining tasks in queue: {_queuer.Count}");
                }
            }
        }
    }
}