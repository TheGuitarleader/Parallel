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
        private int _percentage;
        private int _current;
        private int _total;

        /// <inheritdoc />
        public void Report(ProgressOperation operation, SystemFile file)
        {
            int num = (int)(_current++ / (double)_total * 100.0 + 0.5);
            if (currentOperation != operation)
            {
                _percentage = -1;
                currentOperation = operation;
            }

            if (_percentage == num || num % 10 != 0) return;
            Log.Information($"{operation}: {_current} out of {_total} ({_percentage}%)");
            _percentage = num;
        }

        /// <inheritdoc />
        public void Reset()
        {
            _current = 0;
        }

        /// <inheritdoc />
        public void Failed(Exception exception, SystemFile file)
        {
            Log.Error($"{exception.GetType().FullName}: {exception.Message} Failed to upload file: '{file.LocalPath}'");
        }
    }
}