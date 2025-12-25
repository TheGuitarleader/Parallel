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
        public void Failed(SystemFile file, string message)
        {
            Log.Error(message);
        }
    }
}