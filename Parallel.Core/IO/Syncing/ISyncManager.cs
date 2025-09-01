// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Database;
using Parallel.Core.Diagnostics;
using Parallel.Core.IO.FileSystem;
using Parallel.Core.Models;
using Parallel.Core.Settings;

namespace Parallel.Core.IO.Syncing
{
    /// <summary>
    /// Defines the methods needed for backing up a file system.
    /// </summary>
    public interface ISyncManager
    {
        /// <summary>
        /// The back-up connection vault.
        /// </summary>
        public VaultConfig Vault { get; }

        /// <summary>
        /// The associated database connection.
        /// </summary>
        IDatabase Database { get; set; }

        /// <summary>
        /// The associated file system connection.
        /// </summary>
        IFileSystem FileSystem { get; set; }

        /// <summary>
        /// Initializes the backup manager by logging into the <see cref="IDatabase"/> and <see cref="IFileSystem"/>
        /// </summary>
        /// <returns></returns>
        bool Initialize();

        /// <summary>
        /// Pushes an array of files to a vault.
        /// </summary>
        /// <param name="files"></param>
        /// <param name="progress"></param>
        Task PushFilesAsync(SystemFile[] files, IProgressReporter progress);

        /// <summary>
        /// Pulls an array of files from a vault.
        /// </summary>
        /// <param name="files"></param>
        /// <param name="progress"></param>
        Task PullFilesAsync(SystemFile[] files, IProgressReporter progress);
    }
}