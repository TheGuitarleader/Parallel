// Copyright 2026 Kyle Ebbinga

namespace Parallel.Service.Tasks
{
    /// <summary>
    /// Represents 
    /// </summary>
    public class QueuedTask
    {
        /// <summary>
        /// The time the task was created.
        /// </summary>
        public DateTime EnqueuedAt { get; }
        
        /// <summary>
        /// The actual <see cref="Task"/> to execute.
        /// </summary>
        public Func<Task> TaskFunc { get; }
        
        /// <summary>
        /// The unique key to use deduplication.
        /// </summary>
        public string Key { get; }

        /// <summary>
        /// The priority of the task.
        /// </summary>
        public int Priority;

        /// <summary>
        /// Creates a new instance of the <see cref="QueuedTask"/> class.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="taskFunc"></param>
        /// <param name="priority"></param>
        public QueuedTask(string key, Func<Task> taskFunc, int priority = 0)
        {
            EnqueuedAt = DateTime.UtcNow;
            Key = key;
            TaskFunc = taskFunc;
            Priority = priority;
        }
    }
}