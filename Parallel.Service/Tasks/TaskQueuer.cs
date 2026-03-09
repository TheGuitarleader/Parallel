// Copyright 2026 Kyle Ebbinga

using System.Collections.Concurrent;
using Parallel.Service.Services;
using Serilog;

namespace Parallel.Service.Tasks
{
    /// <summary>
    /// Represents an asynchronous queue for running tasks.
    /// </summary>
    public class TaskQueuer
    {
        private readonly ILogger<TaskQueuer> _logger;
        private readonly ConcurrentQueue<QueuedTask> _queue = new();
        private readonly ConcurrentDictionary<string, QueuedTask> _tasks = new();
        private readonly SemaphoreSlim _signal = new(0);

        public TaskQueuer(ILogger<TaskQueuer> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Gets the number of elements contained in the <see cref="TaskQueuer"/>.
        /// </summary>
        public int Count => _queue.Count;

        /// <summary>
        /// Gets a value that indicates whether the <see cref="TaskQueuer"/> is empty.
        /// </summary>
        public bool IsEmpty => _queue.IsEmpty;

        /// <summary>
        /// Adds a task to be executed by the <see cref="TaskQueueExecutor"/>.
        /// </summary>
        /// <param name="key">The unique key to prevent</param>
        /// <param name="task">The task to be executed.</param>
        public void Enqueue(string key, Func<Task> task)
        {
            ArgumentNullException.ThrowIfNull(task);
            Func<Task> safeTask = async () =>
            {
                try
                {
                    await task();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "A queued task failed");
                }
            };
            
            _tasks.AddOrUpdate(key, k =>
            {
                QueuedTask st = new QueuedTask(key, safeTask);
                _queue.Enqueue(st);
                _signal.Release();
                return st;
            }, (k, existing) =>
            {
                Interlocked.Increment(ref existing.Priority);
                return existing;
            });
        }

        public async Task<Func<Task>> WaitAsync(CancellationToken ct)
        {
            await _signal.WaitAsync(ct);
            while (true)
            {
                if (!_queue.TryDequeue(out QueuedTask? candidate)) continue;
                QueuedTask next = _tasks.Values.OrderByDescending(t => t.Priority + (DateTime.UtcNow - t.EnqueuedAt).TotalSeconds * 0.1).First();
                if (ReferenceEquals(candidate, next))
                {
                    _logger.LogDebug($"Starting task with key: '{candidate.Key}', remaining: {_queue.Count - 1}");
                    _tasks.TryRemove(candidate.Key, out _);
                    return candidate.TaskFunc;
                }

                _queue.Enqueue(candidate);
            }
        }
    }
}