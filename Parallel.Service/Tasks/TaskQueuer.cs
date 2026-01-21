// Copyright 2026 Kyle Ebbinga

using System.Collections.Concurrent;

namespace Parallel.Service.Tasks
{
    /// <summary>
    /// Represents an asynchronous queue for running tasks.
    /// </summary>
    public class TaskQueuer
    {
        private readonly ConcurrentQueue<Func<CancellationToken, Task>> _queue = new();
        private readonly SemaphoreSlim _signal = new(0);

        /// <summary>
        /// Gets the number of elements contained in the <see cref="TaskQueuer"/>.
        /// </summary>
        public int Count => _queue.Count;

        /// <summary>
        /// Gets a value that indicates whether the <see cref="TaskQueuer"/> is empty.
        /// </summary>
        public bool IsEmpty => _queue.IsEmpty;

        public ValueTask AddAsync(Func<CancellationToken, Task> task)
        {
            ArgumentNullException.ThrowIfNull(task);

            _queue.Enqueue(task);
            _signal.Release();
            return ValueTask.CompletedTask;
        }

        public async Task<Func<CancellationToken, Task>> WaitAsync(CancellationToken ct)
        {
            await _signal.WaitAsync(ct);
            _queue.TryDequeue(out Func<CancellationToken, Task>? task);
            return task!;
        }
    }
}