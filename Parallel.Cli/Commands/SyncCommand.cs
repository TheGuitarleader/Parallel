// Copyright 2026 Kyle Ebbinga

using System.CommandLine;
using System.Diagnostics;
using Parallel.Cli.Utils;
using Parallel.Core.IO;
using Parallel.Core.IO.Scanning;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Models;
using Parallel.Core.Settings;

namespace Parallel.Cli.Commands
{
    public class SyncCommand : Command
    {
        private Stopwatch _sw = new Stopwatch();

        private readonly Argument<string> _sourceArg = new("path", "The path to add or remove.");
        private readonly Option<string> _sourceOpt = new(["--path", "-p"], "The source path to back up.");
        private readonly Option<string> _configOpt = new(["--config", "-c"], "The vault configuration to use.");
        private readonly Option<bool> _forceOpt = new(["--force", "-f"], "Forces overwriting any files.");

        private readonly Command addCmd = new("add", "Adds a new directory to the backup list.");
        private readonly Command listCmd = new("list", "Shows all directories in the backup list.");
        private readonly Command removeCmd = new("remove", "Removes a directory from the backup list.");

        public SyncCommand() : base("sync", "Syncs the system with the vaults.")
        {
            this.AddOption(_sourceOpt);
            this.AddOption(_configOpt);
            this.AddOption(_forceOpt);
            this.SetHandler(HandleBackupAsync, _sourceOpt, _configOpt, _forceOpt);

            this.AddCommand(addCmd);
            addCmd.AddArgument(_sourceArg);
            addCmd.AddOption(_configOpt);
            addCmd.SetHandler(HandleAddAsync, _sourceArg, _configOpt);

            this.AddCommand(removeCmd);
            removeCmd.AddArgument(_sourceArg);
            removeCmd.AddOption(_configOpt);
            removeCmd.SetHandler(HandleRemoveAsync, _sourceArg, _configOpt);
        }

        #region Backup

        private async Task HandleBackupAsync(string? path, string? config, bool force)
        {
            _sw = Stopwatch.StartNew();
            LocalVaultConfig? localVault = ParallelConfig.GetVault(config);
            if (localVault != null)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    await BackupPathAsync(localVault, path, force);
                }
                else
                {
                    await BackupSystemAsync(localVault, force);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(path))
                {
                    await Program.Settings.ForEachVaultAsync(vault => BackupPathAsync(vault, path, force));
                }
                else
                {
                    await Program.Settings.ForEachVaultAsync(vault => BackupSystemAsync(vault, force));
                }
            }
        }

        private async Task BackupSystemAsync(LocalVaultConfig vault, bool force)
        {
            ISyncManager? syncManager = SyncManager.CreateNew(vault);
            if (syncManager == null || !await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, "Failed to connect to vault!", ConsoleColor.Red);
                return;
            }

            try
            {
                foreach (string path in syncManager.RemoteVault.BackupDirectories)
                {
                    await BackupInternalAsync(syncManager, path, force);
                }
            }
            finally
            {
                await syncManager.DisconnectAsync();
            }
        }

        private async Task BackupPathAsync(LocalVaultConfig vault, string path, bool force)
        {
            ISyncManager? syncManager = SyncManager.CreateNew(vault);
            if (syncManager == null || !await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, "Failed to connect to vault!", ConsoleColor.Red);
                return;
            }

            try
            {
                await BackupInternalAsync(syncManager, path, force);
            }
            finally
            {
                await syncManager.DisconnectAsync();
            }
        }

        private async Task BackupInternalAsync(ISyncManager syncManager, string path, bool force)
        {
            // Normalize paths for safe comparison
            string[] backupFolders = syncManager.RemoteVault.BackupDirectories.ToArray();
            string[] ignoredFolders = syncManager.RemoteVault.IgnoreDirectories.ToArray();

            bool isFile = PathBuilder.IsFile(path);
            if (!PathBuilder.IsDirectory(path) && !isFile)
            {
                CommandLine.WriteLine(syncManager.RemoteVault, $"The provided path is invalid!", ConsoleColor.Yellow);
                return;
            }

            if (!backupFolders.Any(dir => path.StartsWith(dir, StringComparison.OrdinalIgnoreCase)) || FileScanner.IsIgnored(path, ignoredFolders))
            {
                CommandLine.WriteLine(syncManager.RemoteVault, $"The provided {(isFile ? "file" : "folder")} is set to be ignored!", ConsoleColor.Yellow);
                return;
            }

            CommandLine.WriteLine(syncManager.RemoteVault, $"Scanning for file changes in {path}...", ConsoleColor.DarkGray);
            FileScanner scanner = new FileScanner(syncManager);
            SystemFile[] files = await scanner.GetFileChangesAsync(path, ignoredFolders, force);
            int successFiles = files.Length;
            if (successFiles == 0)
            {
                CommandLine.WriteLine(syncManager.RemoteVault, $"The provided {(isFile ? "file" : "folder")} is already up to date.", ConsoleColor.Green);
                return;
            }

            CommandLine.WriteLine(syncManager.RemoteVault, $"Syncing up {files.Length:N0} files...", ConsoleColor.DarkGray);
            int backedUpFiles = await syncManager.BackupFilesAsync(files, new ProgressReport(syncManager.RemoteVault, successFiles), force);
            CommandLine.WriteLine(syncManager.RemoteVault, $"Successfully synced {backedUpFiles:N0} files in {_sw.Elapsed}.", ConsoleColor.Green);
        }

        #endregion

        #region Add

        private async Task HandleAddAsync(string path, string? config)
        {
            LocalVaultConfig? localVault = string.IsNullOrEmpty(config) ? ParallelConfig.Load().Vaults.FirstOrDefault(v => v.Enabled) : ParallelConfig.GetVault(config);
            if (!string.IsNullOrEmpty(config) && localVault != null)
            {
                await AddPathAsync(localVault, path);
            }
            else
            {
                await Program.Settings.ForEachVaultAsync(vault => AddPathAsync(vault, path));
            }
        }

        private async Task AddPathAsync(LocalVaultConfig vault, string path)
        {
            ISyncManager? syncManager = SyncManager.CreateNew(vault);
            if (syncManager == null || !await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, "Failed to connect to vault!", ConsoleColor.Red);
                return;
            }

            if (!syncManager.RemoteVault.BackupDirectories.Add(path))
            {
                CommandLine.WriteLine(vault, $"Unable to add path: '{path}'", ConsoleColor.Yellow);
                return;
            }

            CommandLine.WriteLine(vault, $"Successfully added '{path}'", ConsoleColor.Green);
            await syncManager.DisconnectAsync();
        }

        #endregion

        #region Remove

        private async Task HandleRemoveAsync(string path, string? config)
        {
            LocalVaultConfig? localVault = string.IsNullOrEmpty(config) ? ParallelConfig.Load().Vaults.FirstOrDefault(v => v.Enabled) : ParallelConfig.GetVault(config);
            if (!string.IsNullOrEmpty(config) && localVault != null)
            {
                await RemovePathAsync(localVault, path);
            }
            else
            {
                await Program.Settings.ForEachVaultAsync(vault => RemovePathAsync(vault, path));
            }
        }

        private async Task RemovePathAsync(LocalVaultConfig vault, string path)
        {
            ISyncManager? syncManager = SyncManager.CreateNew(vault);
            if (syncManager == null || !await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, "Failed to connect to vault!", ConsoleColor.Red);
                return;
            }

            if (!syncManager.RemoteVault.BackupDirectories.Remove(path))
            {
                CommandLine.WriteLine(vault, $"Unable to remove path: '{path}'", ConsoleColor.Yellow);
                return;
            }

            CommandLine.WriteLine(vault, $"Successfully removed '{path}'", ConsoleColor.Green);
            await syncManager.DisconnectAsync();
        }

        #endregion
    }
}