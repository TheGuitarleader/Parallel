// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Models;

namespace Parallel.Core.Diagnostics
{
    /// <summary>
    /// Represents a basic progress report debugger.
    /// </summary>
    public class ProgressLogger : IProgressReporter
    {
        private ProgressOperation currentOperation;
        private int progressPercentage;

        /// <inheritdoc />
        public void Report(ProgressOperation operation, SystemFile file, int current, int total)
        {
            int num = (int)(current / (double)total * 100.0 + 0.5);
            if (currentOperation != operation)
            {
                progressPercentage = -1;
                currentOperation = operation;
            }

            if (progressPercentage == num || num % 10 != 0) return;
            Log.Information($"{operation}: {current} out of {total} ({progressPercentage}%)");
            progressPercentage = num;
        }

        /// <inheritdoc />
        public void Failed(Exception exception, SystemFile file)
        {
            Log.Error($"{exception.GetType().FullName}: {exception.Message}. Failed to upload file: '{file.LocalPath}'");
        }
    }
}