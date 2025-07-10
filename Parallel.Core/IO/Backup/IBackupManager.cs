// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Database;
using Parallel.Core.Diagnostics;
using Parallel.Core.Events;
using Parallel.Core.IO.FileSystem;
using Parallel.Core.Models;
using Parallel.Core.Settings;

namespace Parallel.Core.IO.Backup
{
    /// <summary>
    /// Defines the methods needed for backing up a file system.
    /// </summary>
    public interface IBackupManager
    {
        /// <summary>
        /// The back-up connection profile.
        /// </summary>
        public ProfileConfig Profile { get; }

        /// <summary>
        /// The associated database connection.
        /// </summary>
        IDatabase Database { get; set; }

        /// <summary>
        /// The associated file system connection.
        /// </summary>
        IFileSystem FileSystem { get; set; }

        /// <summary>
        /// The current machine name.
        /// </summary>
        string MachineName { get; }

        /// <summary>
        /// The root directory of the back-up.
        /// </summary>
        string RootFolder { get; set; }


        /// <summary>
        /// Initializes the backup manager by logging into the <see cref="IDatabase"/> and <see cref="IFileSystem"/>
        /// </summary>
        /// <returns></returns>
        bool Initialize();

        /// <summary>
        /// Backs up an array of files as fast as possible.
        /// </summary>
        /// <param name="files"></param>
        /// <param name="progress"></param>
        Task BackupFilesAsync(SystemFile[] files, IProgress progress);

        /// <summary>
        /// Restores an array of files as fast as possible.
        /// </summary>
        /// <param name="files"></param>
        /// <param name="progress"></param>
        Task RestoreFilesAsync(SystemFile[] files, IProgress progress);
    }
}