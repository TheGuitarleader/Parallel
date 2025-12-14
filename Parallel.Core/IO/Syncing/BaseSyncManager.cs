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
        public IStorageProvider StorageProvider { get; set; }

        /// <summary>
        ///
        /// </summary>
        /// <param name="localVault"></param>
        public BaseSyncManager(LocalVaultConfig localVault)
        {
            StorageProvider = StorageConnection.CreateNew(localVault);
            LocalVault = localVault;
        }

        /// <inheritdoc />
        public async Task<bool> ConnectAsync()
        {
            Log.Debug($"[{LocalVault.Id}] Connecting...");
            string root = PathBuilder.GetRootDirectory(LocalVault);
            if (!await StorageProvider.ExistsAsync(root))
            {
                await StorageProvider.CreateDirectoryAsync(root);
                Log.Debug($"Created root directory: {root}");
            }

            if (!await StorageProvider.ExistsAsync(PathBuilder.GetConfigurationFile(LocalVault)))
            {
                RemoteVault = new RemoteVaultConfig(LocalVault);
                RemoteVault.IgnoreDirectories.Add(PathBuilder.GetRootDirectory(LocalVault));
                RemoteVault.Save(TempConfigFile);

                Log.Debug($"Created config file: {TempConfigFile}");
            }
            else
            {
                await StorageProvider.DownloadFileAsync(new SystemFile(TempConfigFile, PathBuilder.GetConfigurationFile(LocalVault)));
                RemoteVaultConfig? config = RemoteVaultConfig.Load(TempConfigFile);
                if (config == null) return false;
                RemoteVault = config;

                Log.Debug($"Downloaded config file: {TempConfigFile}");
            }

            if (!await StorageProvider.ExistsAsync(PathBuilder.GetDatabaseFile(LocalVault)))
            {
                if (File.Exists(TempDbFile)) File.Delete(TempDbFile);
                Database = new SqliteContext(TempDbFile);
                await Database.InitializeAsync();

                Log.Debug($"Create db file: {TempDbFile}");
            }
            else
            {
                await StorageProvider.DownloadFileAsync(new SystemFile(TempDbFile, PathBuilder.GetDatabaseFile(LocalVault)));
                Database = new SqliteContext(TempDbFile);

                Log.Debug($"Downloaded db file: {TempDbFile}");
            }

            Log.Information($"[{LocalVault.Id}] Connected");
            return true;
        }

        /// <inheritdoc />
        public async Task DisconnectAsync()
        {
            Log.Debug($"[{LocalVault.Id}] Disconnecting...");
            SystemFile[] tempFiles = [new SystemFile(TempConfigFile, PathBuilder.GetConfigurationFile(LocalVault)), new SystemFile(TempDbFile, PathBuilder.GetDatabaseFile(LocalVault))];
            foreach (SystemFile file in tempFiles) await StorageProvider.UploadFileAsync(file, true);
            StorageProvider?.Dispose();

            Log.Information($"[{LocalVault.Id}] Disconnected");
        }

        /// <inheritdoc />
        public abstract Task<int> PushFilesAsync(SystemFile[] files, IProgressReporter progress);

        /// <inheritdoc />
        public abstract Task<int> PullFilesAsync(SystemFile[] files, IProgressReporter progress);
    }
}