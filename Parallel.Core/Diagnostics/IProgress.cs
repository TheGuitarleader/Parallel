// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Models;

namespace Parallel.Core.Diagnostics
{
    public enum ProgressOperation
    {
        Archiving,
        Downloading,
        Uploading,
        Compressing,
        Decompressing,
        Syncing
    }

    /// <summary>
    /// Defines a provider for progress updates.
    /// </summary>
    public interface IProgress
    {
        /// <summary>
        /// Reports a progress update.
        /// </summary>
        void Report(ProgressOperation operation, SystemFile file, int current, int total);

        /// <summary>
        /// Reports a failed update.
        /// </summary>
        void Failed(Exception exception, SystemFile file);
    }
}