// Copyright 2026 Kyle Ebbinga

using System.CommandLine;
using System.Diagnostics;
using Parallel.Cli.Utils;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Models;
using Parallel.Core.Settings;
using Parallel.Core.Utils;

namespace Parallel.Cli.Commands
{
    public class PruneCommand : Command
    {
        private Stopwatch _sw = new Stopwatch();

        private readonly Option<string> _sourceOpt = new(["--path", "-p"], "The source path to clean.");
        private readonly Option<string> _configOpt = new(["--config", "-c"], "The vault configuration to use.");
        private readonly Option<DateTime> _beforeOpt = new(["--before"], "Prune files before a certain timestamp.");
        private readonly Option<int> _daysOpt = new(["--days", "-d"], "The amount of days to hang onto files.");
        private readonly Option<bool> _forceOpt = new(["--force", "-f"], "Forces pruning, bypassing safe guards.");

        public PruneCommand() : base("prune", "Prunes the oldest files from vaults.")
        {
            this.AddOption(_sourceOpt);
            this.AddOption(_configOpt);
            this.AddOption(_beforeOpt);
            this.AddOption(_daysOpt);
            this.AddOption(_forceOpt);
            this.SetHandler(HandlePruneAsync, _sourceOpt, _configOpt, _beforeOpt, _daysOpt, _forceOpt);
        }

        private async Task HandlePruneAsync(string? path, string? config, DateTime before, int days, bool force)
        {
            _sw = Stopwatch.StartNew();
            LocalVaultConfig? localVault = ParallelConfig.GetVault(config);
            if (localVault != null)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    await PrunePathAsync(localVault, path, before, days, force);
                }
                else
                {
                    await PruneSystemAsync(localVault, before, days, force);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(path))
                {
                    await Program.Settings.ForEachVaultAsync(vault => PrunePathAsync(vault, path, before, days, force));
                }
                else
                {
                    await Program.Settings.ForEachVaultAsync(vault => PruneSystemAsync(vault, before, days, force));
                }
            }
        }

        private DateTime GetPruneDateTime(int prunePeriod, DateTime before, int days)
        {
            if (before > DateTime.MinValue)
            {
                return before.AddTicks(-1);
            }

            return days > 0 ? DateTime.Now.AddDays(-days) : DateTime.Now.AddDays(-prunePeriod);
        }

        private async Task PruneSystemAsync(LocalVaultConfig vault, DateTime before, int days, bool force)
        {
            ISyncManager? syncManager = SyncManager.CreateNew(vault);
            if (syncManager == null || !await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, "Failed to connect to vault!", ConsoleColor.Red);
                return;
            }

            try
            {
                DateTime timestamp = GetPruneDateTime(syncManager.RemoteVault.PrunePeriod, before, days);
                foreach (string path in syncManager.RemoteVault.PruneDirectories)
                {
                    await PruneInternalAsync(syncManager, path, timestamp, force);
                }
            }
            finally
            {
                await syncManager.DisconnectAsync();
            }
        }

        private async Task PrunePathAsync(LocalVaultConfig vault, string path, DateTime before, int days, bool force)
        {
            ISyncManager? syncManager = SyncManager.CreateNew(vault);
            if (syncManager == null || !await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, "Failed to connect to vault!", ConsoleColor.Red);
                return;
            }

            try
            {
                DateTime timestamp = GetPruneDateTime(syncManager.RemoteVault.PrunePeriod, before, days);
                await PruneInternalAsync(syncManager, path, timestamp, force);
            }
            finally
            {
                await syncManager.DisconnectAsync();
            }
        }

        private async Task PruneInternalAsync(ISyncManager syncManager, string path, DateTime timestamp, bool force)
        {
            CommandLine.WriteLine(syncManager.RemoteVault, $"Scanning for files in {path}...", ConsoleColor.DarkGray);
            IReadOnlyList<SystemFile> files = await (syncManager.Database?.GetFilesAsync(path, timestamp, true) ?? Task.FromResult<IReadOnlyList<SystemFile>>([]));

            if (files.Count == 0)
            {
                CommandLine.WriteLine($"No prunable files were found!", ConsoleColor.Yellow);
                return;
            }

            if (!force)
            {
                CommandLine.WriteLine($"This will permanently delete {files.Count:N0} files!", ConsoleColor.Yellow);
                if (!CommandLine.ReadBool("Do you wish to continue? [yes/no]", false))
                {
                    CommandLine.WriteLine($"Successfully cancelled operation.", ConsoleColor.Green);
                    return;
                }
            }

            CommandLine.WriteLine(syncManager.RemoteVault, $"Pruning {files.Count:N0} files...", ConsoleColor.DarkGray);
            int prunedFiles = await syncManager.PruneFilesAsync(files, new ProgressReport(syncManager.RemoteVault, files.Count));
            CommandLine.WriteLine(syncManager.RemoteVault, $"Successfully pruned {prunedFiles:N0} files in {_sw.Elapsed}.", ConsoleColor.Green);
        }
    }
}