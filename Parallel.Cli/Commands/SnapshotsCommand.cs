// Copyright 2026 Kyle Ebbinga

using System.Collections.Concurrent;
using System.CommandLine;
using System.Diagnostics;
using Parallel.Cli.Utils;
using Parallel.Core.IO;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Models;
using Parallel.Core.Settings;
using Parallel.Core.Utils;

namespace Parallel.Cli.Commands
{
    public class SnapshotsCommand : Command
    {
        private Stopwatch _sw = new Stopwatch();
        
        private readonly Option<string> _configOpt = new(["--config", "-c"], "The vault configuration to use.");
        private readonly Option<string> _nameOpt = new(["--name", "-n"], "The name of the snapshot.");
        private readonly Option<string> _jsonOpt = new(["--json", "-j"], "The json path of the snapshot.");
        private readonly Option<DateTime> _timeOpt = new(["--time", "-t"], "The timestamp of the snapshot.");
        
        private readonly Command createCmd = new("create", "Creates a new system snapshot.");
        private readonly Command listCmd = new("list", "Lists available system snapshots.");
        private readonly Command restoreCmd = new("restore", "Restores a system snapshot.");
        
        public SnapshotsCommand() : base("snapshots", "Manages system snapshots.")
        {
            this.AddCommand(createCmd);
            createCmd.AddOption(_configOpt);
            createCmd.SetHandler(HandleCreateSnapshotAsync, _configOpt);
            
            this.AddCommand(listCmd);
            listCmd.AddOption(_configOpt);
            listCmd.SetHandler(HandleListSnapshotsAsync, _configOpt);
            
            this.AddCommand(restoreCmd);
            restoreCmd.AddOption(_configOpt);
            restoreCmd.AddOption(_nameOpt);
            restoreCmd.AddOption(_jsonOpt);
            restoreCmd.AddOption(_timeOpt);
            restoreCmd.SetHandler(HandleRestoreSnapshotAsync, _configOpt, _nameOpt, _jsonOpt, _timeOpt);
        }

        private async Task HandleCreateSnapshotAsync(string? config)
        {
            _sw = Stopwatch.StartNew();
            LocalVaultConfig? localVault = string.IsNullOrEmpty(config) ? ParallelConfig.Load().Vaults.FirstOrDefault(v => v.Enabled) : ParallelConfig.GetVault(config);
            if (!string.IsNullOrEmpty(config) && localVault != null)
            {
                await CreateSnapshotAsync(localVault);
            }
            else
            {
                await Program.Settings.ForEachVaultAsync(CreateSnapshotAsync);
            }
        }
        
        private async Task HandleListSnapshotsAsync(string? config)
        {
            LocalVaultConfig? localVault = string.IsNullOrEmpty(config) ? ParallelConfig.Load().Vaults.FirstOrDefault(v => v.Enabled) : ParallelConfig.GetVault(config);
            if (localVault == null)
            {
                CommandLine.WriteLine($"No vault was found!", ConsoleColor.Yellow);
                return;
            }
            
            CommandLine.WriteLine($"Retrieving vault information...", ConsoleColor.DarkGray);
            ISyncManager? syncManager = SyncManager.CreateNew(localVault);
            if (syncManager == null || !await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(localVault, "Failed to connect to vault!", ConsoleColor.Red);
                return;
            }

            IReadOnlyList<string> snapshots = await (syncManager.Database?.GetSnapshotsAsync() ?? Task.FromResult<IReadOnlyList<string>>([]));
            if (!snapshots.Any())
            {
                CommandLine.WriteLine($"No snapshots were found!", ConsoleColor.Yellow);
                return;
            }
            
            CommandLine.WriteArray("Available snapshots", snapshots);
        }
        
        private async Task HandleRestoreSnapshotAsync(string? config, string? name, string? jsonPath, DateTime time)
        {
            _sw = Stopwatch.StartNew();
            DateTime timestamp = time != DateTime.MinValue ? time.AddMinutes(1).AddTicks(-1) : DateTime.Now;
            LocalVaultConfig? localVault = ParallelConfig.GetVault(config);
            if (localVault != null)
            {
                //await RestoreSystemAsync(localVault, timestamp, remap, archive, force);
            }
            else
            {
                //await Program.Settings.ForEachVaultAsync(vault => RestoreSystemAsync(vault, timestamp, remap, archive, force));
            }
        }
        
        private async Task CreateSnapshotAsync(LocalVaultConfig vault)
        {
            ISyncManager? syncManager = SyncManager.CreateNew(vault);
            if (syncManager == null || !await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, "Failed to connect to vault!", ConsoleColor.Red);
                return;
            }

            ConcurrentBag<Snapshot> snapshots = new();
            await System.Threading.Tasks.Parallel.ForEachAsync(syncManager.RemoteVault.BackupDirectories, ParallelConfig.Options, async (path, ct) =>
            {
                IReadOnlyList<LocalFile> files = await (syncManager.Database?.GetLatestFilesAsync(path, DateTime.UtcNow, false) ?? Task.FromResult<IReadOnlyList<LocalFile>>([]));
                foreach (LocalFile file in files) snapshots.Add(new Snapshot(file));
            });
            
            Log.Debug($"Found {snapshots.Count} files");
            string snapshotFilename = $"snapshot_{UnixTime.Now.TotalMilliseconds}";
            string localSnapshotFile = Path.Combine(PathBuilder.TempDirectory, snapshotFilename + ".json");
            string remoteSnapshotFile = PathBuilder.Combine(PathBuilder.GetSnapshotsDirectory(syncManager.LocalVault), snapshotFilename);
            
            await File.WriteAllTextAsync(localSnapshotFile, JsonConvert.SerializeObject(snapshots));
            await syncManager.StorageProvider.UploadFileAsync(localSnapshotFile, remoteSnapshotFile, false);
            if (!await (syncManager.Database?.AddSnapshotAsync(snapshotFilename) ?? Task.FromResult(false))) Log.Error($"Failed to add snapshot: {snapshotFilename}");
            CommandLine.WriteLine(syncManager.LocalVault, $"Successfully created snapshot: {snapshotFilename}", ConsoleColor.Green);
            await syncManager.DisconnectAsync();
        }
        
        private async Task RestoreSnapshotAsync(LocalVaultConfig vault)
        {
            
        }
    }
}