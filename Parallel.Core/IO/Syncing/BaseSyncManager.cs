// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Database;
using Parallel.Core.Diagnostics;
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
        protected string TempDirectory = PathBuilder.TempDirectory;
        protected string TempConfigFile => Path.Combine(TempDirectory, $"{LocalVault.Id}.json");
        protected string TempDbFile => Path.Combine(TempDirectory, $"{LocalVault.Id}.db");

        /// <inheritdoc />
        public LocalVaultConfig LocalVault { get; private set; }

        /// <inheritdoc />
        public RemoteVaultConfig RemoteVault { get; private set; }

        /// <inheritdoc />
        public IDatabase Database { get; set; }

        /// <inheritdoc />
        public IFileSystem FileSystem { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="remoteVaultConfig"></param>
        public BaseSyncManager(LocalVaultConfig localVault)
        {
            FileSystem = FileSystemManager.CreateNew(localVault);
            LocalVault = localVault;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            FileSystem.Dispose();
        }

        /// <inheritdoc />
        public async Task<bool> InitializeAsync()
        {
            try
            {
                string root = PathBuilder.GetRootDirectory(LocalVault);
                if (!await FileSystem.ExistsAsync(root))
                {
                    // Creates the root directory and default configuration.
                    await FileSystem.CreateDirectoryAsync(root);
                    RemoteVault = new RemoteVaultConfig(LocalVault);
                }
                else
                {
                    SystemFile[] files =
                    [
                        new SystemFile(TempConfigFile) { RemotePath = PathBuilder.GetConfigurationFile(LocalVault) },
                        new SystemFile(TempDbFile) { RemotePath = PathBuilder.GetDatabaseFile(LocalVault) },
                    ];

                    await FileSystem.DownloadFilesAsync(files, new ProgressLogger());
                }

                Database = new SqliteContext(LocalVault);
                RemoteVault.IgnoreDirectories.Add(PathBuilder.GetRootDirectory(LocalVault));
                return true;
            }
            catch (Exception ex)
            {
                Log.Error(ex.GetBaseException().ToString());
                return false;
            }
        }

        /// <inheritdoc />
        public Task DisconnectAsync()
        {
            string configFile = PathBuilder.GetConfigurationFile(LocalVault);

            FileSystem.Dispose();
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public abstract Task PushFilesAsync(SystemFile[] files, IProgressReporter progress);

        /// <inheritdoc />
        public abstract Task PullFilesAsync(SystemFile[] files, IProgressReporter progress);
    }
}