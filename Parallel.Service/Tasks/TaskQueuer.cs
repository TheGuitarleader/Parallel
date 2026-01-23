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
        private readonly ConcurrentStack<Func<Task>> _stack = new();
        private readonly SemaphoreSlim _signal = new(0);

        /// <summary>
        /// Gets the number of elements contained in the <see cref="TaskQueuer"/>.
        /// </summary>
        public int Count => _stack.Count;

        /// <summary>
        /// Gets a value that indicates whether the <see cref="TaskQueuer"/> is empty.
        /// </summary>
        public bool IsEmpty => _stack.IsEmpty;

        /// <summary>
        /// Adds a task to be executed by the <see cref="TaskQueueExecutor"/>.
        /// </summary>
        /// <param name="task">The task to be executed.</param>
        public void Enqueue(Func<Task> task)
        {
            ArgumentNullException.ThrowIfNull(task);
            _stack.Push(task);
            _signal.Release();
        }

        public async Task<Func<Task>> WaitAsync(CancellationToken ct)
        {
            await _signal.WaitAsync(ct);
            if (_stack.TryPop(out Func<Task>? task)) return task;
            throw new InvalidOperationException("Signal received but no task was available.");
        }
    }
}