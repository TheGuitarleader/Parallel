// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Models;

namespace Parallel.Core.Diagnostics
{
    /// <summary>
    /// Represents a null <see cref="IProgressReporter"/>.
    /// </summary>
    public class NullProgressReporter : IProgressReporter
    {
        /// <inheritdoc />
        public void Report(ProgressOperation operation, SystemFile file) { }

        /// <inheritdoc />
        public void Reset() { }

        /// <inheritdoc />
        public void Failed(Exception exception, SystemFile file)
        {
            Log.Error($"{exception.GetType().FullName}: {exception.Message}");
        }
    }
}