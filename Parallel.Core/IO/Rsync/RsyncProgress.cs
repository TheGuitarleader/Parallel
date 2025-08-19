// Copyright 2025 Kyle Ebbinga

using FastRsync.Diagnostics;

namespace Parallel.Core.IO.Rsync
{
    /// <inheritdoc />
    public class RsyncProgress : IProgress<ProgressReport>
    {
        private ProgressOperationType currentOperation;
        private int progressPercentage;


        /// <inheritdoc />
        public void Report(ProgressReport progress)
        {
            int num = (int)(progress.CurrentPosition / (double)progress.Total * 100.0 + 0.5);
            if (currentOperation != progress.Operation)
            {
                progressPercentage = -1;
                currentOperation = progress.Operation;
            }

            if (progressPercentage != num && num % 10 == 0)
            {
                progressPercentage = num;
                Log.Information($"{progress.Operation}: {progress.CurrentPosition} out of {progress.Total} ({progressPercentage}%)");
            }
        }
    }
}