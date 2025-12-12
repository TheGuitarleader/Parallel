// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Database;
using Parallel.Core.Diagnostics;
using Parallel.Core.IO.FileSystem;
using Parallel.Core.Models;
using Parallel.Core.Settings;
using Parallel.Core.Storage;

namespace Parallel.Core.IO.Syncing
{
    /// <summary>
    /// Defines the methods needed for backing up a file system.
    /// </summary>
    public interface ISyncManager
    {
        /// <summary>
        /// Gets the local vault configuration.
        /// </summary>
        public LocalVaultConfig LocalVault { get; }

        /// <summary>
        /// Gets the remote vault configuration.
        /// </summary>
        public RemoteVaultConfig RemoteVault { get; }

        /// <summary>
        /// The associated database connection.
        /// </summary>
        IDatabase Database { get; set; }

        /// <summary>
        /// The associated file system connection.
        /// </summary>
        public IStorageProvider StorageProvider { get; set; }

        /// <summary>
        /// Establishes a connection to the associated <see cref="IStorageProvider"/> and downloads the needed files.
        /// </summary>
        Task<bool> ConnectAsync();

        /// <summary>
        /// Closes the current connection and releases its resources.
        /// </summary>
        Task DisconnectAsync();

        /// <summary>
        /// Pushes an array of files to a vault.
        /// </summary>
        /// <param name="files"></param>
        /// <param name="force"></param>
        /// <param name="progress"></param>
        Task PushFilesAsync(SystemFile[] files, bool force, IProgressReporter progress);

        /// <summary>
        /// Pulls an array of files from a vault.
        /// </summary>
        /// <param name="files"></param>
        /// <param name="progress"></param>
        Task PullFilesAsync(SystemFile[] files, IProgressReporter progress);
    }
}