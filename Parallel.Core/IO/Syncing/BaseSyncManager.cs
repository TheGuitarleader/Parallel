// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Database;
using Parallel.Core.Diagnostics;
using Parallel.Core.IO.FileSystem;
using Parallel.Core.Models;
using Parallel.Core.Settings;

namespace Parallel.Core.IO.Syncing
{
    /// <summary>
    /// Represents the base functionality for syncing files to an associated file system.
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
        public async Task<bool> ConnectAsync()
        {
            string root = PathBuilder.GetRootDirectory(LocalVault);
            if (!await FileSystem.ExistsAsync(root))
            {
                await FileSystem.CreateDirectoryAsync(root);
                Log.Debug($"Created root directory: {root}");
            }

            if (!await FileSystem.ExistsAsync(PathBuilder.GetConfigurationFile(LocalVault)))
            {
                RemoteVault = new RemoteVaultConfig(LocalVault);
                RemoteVault.IgnoreDirectories.Add(PathBuilder.GetRootDirectory(LocalVault));
                RemoteVault.Save(TempConfigFile);

                Log.Debug($"Created config file: {TempConfigFile}");
            }
            else
            {
                await FileSystem.DownloadFilesAsync([new SystemFile(TempConfigFile, PathBuilder.GetConfigurationFile(LocalVault))], new ProgressLogger());
                RemoteVaultConfig? config = RemoteVaultConfig.Load(TempConfigFile);
                if(config == null) return false;
                RemoteVault = config;

                Log.Debug($"Downloaded config file: {TempConfigFile}");
            }

            if (!await FileSystem.ExistsAsync(PathBuilder.GetDatabaseFile(LocalVault)))
            {
                Database = new SqliteContext(TempDbFile);
                await Database.InitializeAsync();

                Log.Debug($"Create db file: {TempDbFile}");
            }
            else
            {
                await FileSystem.DownloadFilesAsync([new SystemFile(TempDbFile, PathBuilder.GetDatabaseFile(LocalVault))], new ProgressLogger());
                Database = new SqliteContext(TempDbFile);

                Log.Debug($"Downloaded db file: {TempDbFile}");
            }

            return true;
        }

        /// <inheritdoc />
        public async Task DisconnectAsync()
        {
            Log.Debug($"Uploaded config file: {TempConfigFile}");
            Log.Debug($"Uploaded db file: {TempDbFile}");

            SystemFile[] tempFiles = [new SystemFile(TempConfigFile, PathBuilder.GetConfigurationFile(LocalVault)), new SystemFile(TempDbFile, PathBuilder.GetDatabaseFile(LocalVault))];
            await FileSystem.UploadFilesAsync(tempFiles, new ProgressLogger());
            FileSystem.Dispose();
        }

        /// <inheritdoc />
        public abstract Task PushFilesAsync(SystemFile[] files, IProgressReporter progress);

        /// <inheritdoc />
        public abstract Task PullFilesAsync(SystemFile[] files, IProgressReporter progress);
    }
}