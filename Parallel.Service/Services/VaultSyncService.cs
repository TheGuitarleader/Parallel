// Copyright 2026 Kyle Ebbinga

using System.Collections.Concurrent;
using Parallel.Core.IO.Scanning;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Models;
using Parallel.Core.Settings;
using Parallel.Service.Tasks;
using Parallel.Service.Utils;

namespace Parallel.Service.Services
{
    public class VaultWorker
    {
        public CancellationTokenSource Cts { get; }
        public Task WorkerTask { get; }

        public VaultWorker(Task task, CancellationTokenSource cts)
        {
            WorkerTask = task;
            Cts = cts;
        }
    }

    /// <summary>
    /// Represents the service for syncing files with <see cref="RemoteVaultConfig"/>s.
    /// </summary>
    public class VaultSyncService : BackgroundService
    {
        private readonly ConcurrentDictionary<string, VaultWorker> _vaults = new();
        private readonly ILogger<VaultSyncService> _logger;
        private readonly TaskQueuer _queuer;

        public VaultSyncService(ILogger<VaultSyncService> logger, TaskQueuer queuer)
        {
            _logger = logger;
            _queuer = queuer;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                LocalVaultConfig[] enabledVaults = ParallelConfig.GetEnabledVaults();
                string[] enabledVaultIds = enabledVaults.Select(v => v.Id).ToArray();
                string[] disabledVaults = _vaults.Keys.Where(v => !enabledVaultIds.Contains(v)).ToArray();

                // Adds newly enabled vaults to be synced.
                foreach (LocalVaultConfig vault in enabledVaults)
                {
                    if (_vaults.ContainsKey(vault.Id)) continue;

                    _logger.LogDebug("Adding vault to syncing...");
                    CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                    Task workerTask = TestVaultSyncAsync(new FileSyncManager(vault), cts.Token);
                    VaultWorker worker = new VaultWorker(workerTask, cts);

                    if (!_vaults.TryAdd(vault.Id, worker)) continue;
                    _logger.LogInformation("Added vault to be synced: {VaultId}", vault.Id);
                }

                // Removes disabled vaults from being synced
                foreach (string id in disabledVaults)
                {
                    _logger.LogDebug("Removing vault from syncing...");
                    if (!_vaults.TryGetValue(id, out VaultWorker? worker)) continue;
                    await worker.Cts.CancelAsync();
                    await worker.WorkerTask;
                    _vaults.TryRemove(id, out _);

                    _logger.LogInformation("Removed vault from syncing: {VaultId}", id);
                }

                await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
            }
        }

        private async Task TestVaultSyncAsync(FileSyncManager syncManager, CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                string key = $"vault:sync:{syncManager.LocalVault.Id}";
                _queuer.Enqueue(key, async () =>
                {
                    if (await syncManager.ConnectAsync())
                    {
                        _logger.LogInformation($"Connected to vault: {syncManager.LocalVault.Id}");
                    }
                    else
                    {
                        _logger.LogError($"Failed to connect to vault: {syncManager.LocalVault.Id}");
                    }
                });

                TimeSpan next = TimeSpan.FromMinutes(syncManager.RemoteVault.SyncInterval);
                _logger.LogDebug("Next vault sync for {VaultId} is at {Time}", syncManager.LocalVault.Id, DateTime.UtcNow.Add(next).ToLocalTime().ToString("G"));
                await Task.Delay(next, stoppingToken);
            }
        }

        private async Task SyncVaultAsync(FileSyncManager syncManager, CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                string key = $"vault:sync:{syncManager.LocalVault.Id}";
                _queuer.Enqueue(key, async () =>
                {
                    if (!await syncManager.ConnectAsync())
                    {
                        _logger.LogError($"Failed to connect to vault: {syncManager.LocalVault.Id}");
                        return;
                    }

                    try
                    {
                        List<SystemFile> systemFiles = new();
                        FileScanner scanner = new FileScanner(syncManager);
                        string[] ignoredFolders = syncManager.RemoteVault.IgnoreDirectories.ToArray();
                        foreach (string path in syncManager.RemoteVault.BackupDirectories)
                        {
                            SystemFile[] pathFiles = await scanner.GetFileChangesAsync(path, ignoredFolders, false);
                            if (pathFiles.Length == 0) continue;
                            systemFiles.AddRange(pathFiles);
                        }

                        _logger.LogInformation($"Syncing {systemFiles.Count:N0} files with vault: {syncManager.LocalVault.Id}");
                        await syncManager.BackupFilesAsync(systemFiles, new ConsoleProgressReport(), false);

                        // Prunes old files from the vault.
                        foreach (string path in syncManager.RemoteVault.PruneDirectories)
                        {
                        }
                    }
                    finally
                    {
                        await syncManager.DisconnectAsync();
                    }
                });

                await Task.Delay(TimeSpan.FromMinutes(syncManager.RemoteVault.SyncInterval), stoppingToken);
            }
        }
    }
}