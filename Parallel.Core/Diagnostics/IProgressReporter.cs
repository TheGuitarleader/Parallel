// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Models;

namespace Parallel.Core.Diagnostics
{
    public enum ProgressOperation
    {
        Archived,
        Restored,
        Synced,
        Downloading,
        Uploading,
        Hashing,
        Pruned
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