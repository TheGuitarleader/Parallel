// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Database;
using Parallel.Core.Diagnostics;
using Parallel.Core.IO.Backup;
using Parallel.Core.IO.FileSystem;
using Parallel.Core.Models;
using Parallel.Core.Settings;

namespace Parallel.Core.IO.Syncing
{
    /// <summary>
    /// Represents the base way of backing up files to an associated file system.
    /// </summary>
    public abstract class BaseSyncManager : ISyncManager
    {
        /// <inheritdoc />
        public VaultConfig Vault { get; }

        /// <inheritdoc />
        public IDatabase Database { get; set; }

        /// <inheritdoc />
        public IFileSystem FileSystem { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="vault"></param>
        public BaseSyncManager(VaultConfig vault)
        {
            FileSystem = FileSystemManager.CreateNew(vault.FileSystem);
            Vault = vault;
        }

        /// <inheritdoc />
        public virtual bool Initialize()
        {
            try
            {
                Database = DatabaseConnection.CreateNew(Vault);
                bool fsInit = (FileSystem != null) && FileSystem.PingAsync().Result >= 0;
                Vault.IgnoreDirectories.Add(Vault.FileSystem.RootDirectory);
                if (Vault != null) Vault.SaveToFile();
                return fsInit;
            }
            catch (Exception ex)
            {
                Log.Error(ex.GetBaseException().ToString());
                return false;
            }
        }

        /// <inheritdoc />
        public abstract Task PushFilesAsync(SystemFile[] files, IProgressReporter progress);

        /// <inheritdoc />
        public abstract Task PullFilesAsync(SystemFile[] files, IProgressReporter progress);
    }
}