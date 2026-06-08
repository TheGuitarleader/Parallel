// Copyright 2026 Kyle Ebbinga

using Parallel.Core.Models;

namespace Parallel.Core.Diagnostics
{
    /// <summary>
    /// Represents a null <see cref="IProgressReporter"/>.
    /// </summary>
    public class NullProgressReporter : IProgressReporter
    {
        /// <inheritdoc />
        public void Report(ProgressOperation operation, LocalFile file) { }

        /// <inheritdoc />
        public void Reset() { }

        /// <inheritdoc />
        public void Failed(LocalFile file, string message)
        {
            Log.Error(message);
        }
    }
}