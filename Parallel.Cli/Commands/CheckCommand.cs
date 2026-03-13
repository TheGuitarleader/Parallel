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
    public class CheckCommand : Command
    {
        private Stopwatch _sw = new Stopwatch();

        private readonly Argument<string> _sourceArg = new("path", "The path to add or remove.");
        private readonly Option<string> _sourceOpt = new(["--path", "-p"], "The source path to back up.");
        private readonly Option<string> _configOpt = new(["--config", "-c"], "The vault configuration to use.");

        public CheckCommand() : base("check", "Syncs the system with the vaults.")
        {
            this.AddOption(_sourceOpt);
            this.AddOption(_configOpt);
            this.SetHandler(HandleCleanAsync, _sourceOpt, _configOpt);
        }

        private async Task HandleCleanAsync(string? path, string? config)
        {
            _sw = Stopwatch.StartNew();
            LocalVaultConfig? localVault = ParallelConfig.GetVault(config);
            if (localVault != null)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    await CheckPathAsync(localVault, path);
                }
                else
                {
                    await BackupSystemAsync(localVault);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(path))
                {
                    await Program.Settings.ForEachVaultAsync(vault => CheckPathAsync(vault, path));
                }
                else
                {
                    await Program.Settings.ForEachVaultAsync(BackupSystemAsync);
                }
            }
        }

        private async Task BackupSystemAsync(LocalVaultConfig vault)
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
                    await CheckInternalAsync(syncManager, path);
                }
            }
            finally
            {
                await syncManager.DisconnectAsync();
            }
        }

        private async Task CheckPathAsync(LocalVaultConfig vault, string path)
        {
            ISyncManager? syncManager = SyncManager.CreateNew(vault);
            if (syncManager == null || !await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, "Failed to connect to vault!", ConsoleColor.Red);
                return;
            }

            try
            {
                await CheckInternalAsync(syncManager, path);
            }
            finally
            {
                await syncManager.DisconnectAsync();
            }
        }

        private async Task CheckInternalAsync(ISyncManager syncManager, string path)
        {
            CommandLine.WriteLine(syncManager.RemoteVault, $"Successfully synced files in {_sw.Elapsed}.", ConsoleColor.Green);
        }
    }
}