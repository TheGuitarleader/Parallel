// Copyright 2026 Kyle Ebbinga

namespace Parallel.Service.Tasks
{
    public struct ServiceTask
    {
        public static Task Task { get; set; }
        public static CancellationTokenSource Cts { get; set; }

        public ServiceTask(Task task, CancellationTokenSource cts)
        {
            Task = task;
            Cts = cts;
        }
    }
}