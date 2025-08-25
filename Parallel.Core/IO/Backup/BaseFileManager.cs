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
    /// Represents the base way of backing up files to an associated file system.
    /// </summary>
    public abstract class BaseFileManager : IBackupManager
    {
        /// <inheritdoc />
        public ProfileConfig Profile { get; }

        /// <inheritdoc />
        public IDatabase Database { get; set; }

        /// <inheritdoc />
        public IFileSystem FileSystem { get; set; }

        /// <inheritdoc />
        public string MachineName { get; } = Environment.MachineName;

        /// <inheritdoc />
        public string RootFolder { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="profile"></param>
        public BaseFileManager(ProfileConfig profile)
        {
            FileSystem = FileSystemManager.CreateNew(profile.FileSystem);
            Profile = profile;
        }

        /// <inheritdoc />
        public virtual bool Initialize()
        {
            try
            {
                Database = DatabaseConnection.CreateNew(Profile);
                bool fsInit = (FileSystem != null) && FileSystem.PingAsync().Result >= 0;
                Profile.IgnoreDirectories.Add(Profile.FileSystem.RootDirectory);
                if (Profile != null) Profile.SaveToFile();
                return fsInit;
            }
            catch (Exception ex)
            {
                Log.Error(ex.GetBaseException().ToString());
                return false;
            }
        }

        /// <inheritdoc />
        public abstract Task BackupFilesAsync(SystemFile[] files, IProgressReporter progress);

        /// <inheritdoc />
        public abstract Task RestoreFilesAsync(SystemFile[] files, IProgressReporter progress);
    }
}