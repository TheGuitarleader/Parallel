// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Models;

namespace Parallel.Core.Diagnostics
{
    public enum ProgressOperation
    {
        Archived,
        Pulled,
        Pushed,
        Synced,
        Downloading,
        Uploading,
        Hashing
    }

    /// <summary>
    /// Defines a provider for progress updates.
    /// </summary>
    public interface IProgressReporter
    {
        /// <summary>
        /// Reports a progress update.
        /// </summary>
        void Report(ProgressOperation operation, SystemFile file);

        /// <summary>
        /// Resets the ticking.
        /// </summary>
        void Reset();

        /// <summary>
        /// Reports a failed update.
        /// </summary>
        void Failed(SystemFile file, string reason);
    }
}