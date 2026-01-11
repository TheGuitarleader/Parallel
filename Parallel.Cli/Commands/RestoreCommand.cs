// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Parallel.Cli.Utils;
using Parallel.Core.IO;
using Parallel.Core.IO.Scanning;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Models;
using Parallel.Core.Settings;
using Parallel.Core.Utils;

namespace Parallel.Cli.Commands
{
    public class RestoreCommand : Command
    {
        private Stopwatch _sw = new Stopwatch();
        private int _totalFiles = 0;

        private readonly Option<string> _sourceOpt = new(["--path", "-p"], "The source path to restore.");
        private readonly Option<string> _configOpt = new(["--config", "-c"], "The vault configuration to use.");
        private readonly Option<DateTime> _beforeOpt = new(["--before"], "Restores files before a certain timestamp.");
        private readonly Option<string> _remapOpt = new(["--remap"], "The output directory remapping.");
        private readonly Option<bool> _archiveOpt = new(["--archive"], "Restores archived files.");
        private readonly Option<bool> _forceOpt = new(["--force", "-f"], "Forces overwriting any files.");

        public RestoreCommand() : base("restore", "Restores files from the backup.")
        {
            this.AddOption(_sourceOpt);
            this.AddOption(_configOpt);
            this.AddOption(_beforeOpt);
            this.AddOption(_remapOpt);
            this.AddOption(_archiveOpt);
            this.AddOption(_forceOpt);
            this.SetHandler(HandleRestoreAsync, _sourceOpt, _configOpt, _beforeOpt, _remapOpt, _archiveOpt, _forceOpt);
        }

        private async Task HandleRestoreAsync(string? path, string? config, DateTime before, string? remap, bool archive, bool force)
        {
            _sw = Stopwatch.StartNew();
            DateTime timestamp = before != DateTime.MinValue ? before.AddMinutes(1).AddTicks(-1) : DateTime.Now;
            LocalVaultConfig? localVault = ParallelConfig.GetVault(config);
            if (localVault != null)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    await RestorePathAsync(localVault, path, timestamp, remap, archive, force);
                }
                else
                {
                    await RestoreSystemAsync(localVault, timestamp, remap, archive, force);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(path))
                {
                    await Program.Settings.ForEachVaultAsync(vault => RestorePathAsync(vault, path, timestamp, remap, archive, force));
                }
                else
                {
                    await Program.Settings.ForEachVaultAsync(vault => RestoreSystemAsync(vault, timestamp, remap, archive, force));
                }
            }
        }

        private async Task RestoreSystemAsync(LocalVaultConfig vault, DateTime timestamp, string? output, bool archive, bool force)
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
                    await RestoreInternalAsync(syncManager, path, timestamp, output, archive, force);
                }
            }
            finally
            {
                await syncManager.DisconnectAsync();
            }
        }

        private async Task RestorePathAsync(LocalVaultConfig vault, string path, DateTime timestamp, string? output, bool archive, bool force)
        {
            ISyncManager? syncManager = SyncManager.CreateNew(vault);
            if (syncManager == null || !await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, "Failed to connect to vault!", ConsoleColor.Red);
                return;
            }

            try
            {
                await RestoreInternalAsync(syncManager, path, timestamp, output, archive, force);
            }
            finally
            {
                await syncManager.DisconnectAsync();
            }
        }

        private async Task RestoreInternalAsync(ISyncManager syncManager, string path, DateTime timestamp, string? output, bool archive, bool force)
        {
            string fullPath = Path.GetFullPath(path);
            CommandLine.WriteLine(syncManager.RemoteVault, $"Scanning for files in {path}...", ConsoleColor.DarkGray);
            IReadOnlyList<SystemFile> files = await (syncManager.Database?.GetLatestFilesAsync(fullPath, timestamp, archive) ?? Task.FromResult<IReadOnlyList<SystemFile>>([]));

            List<SystemFile> restoreFiles = new List<SystemFile>();
            System.Threading.Tasks.Parallel.ForEach(files, ParallelConfig.Options, (remoteFile) =>
            {
                string outputPath = remoteFile.LocalPath;
                if (!string.IsNullOrEmpty(output))
                {
                    string relative = Path.GetRelativePath(path, remoteFile.LocalPath);
                    string newPath = Path.Combine(output, relative);
                    Console.WriteLine(newPath);
                    //remoteFile.LocalPath = newPath;
                }

                //SystemFile localFile = new SystemFile(outputPath);
                //if (File.Exists(outputPath) && localFile.LastUpdate.TotalMilliseconds > remoteFile.LastUpdate.TotalMilliseconds && !force) return;
                Console.WriteLine(JObject.FromObject(remoteFile));
                restoreFiles.Add(remoteFile);
            });

            if (restoreFiles.Count == 0)
            {
                CommandLine.WriteLine(syncManager.RemoteVault, $"The provided {(PathBuilder.IsFile(fullPath) ? "file" : "folder")} is already up to date.", ConsoleColor.Green);
                return;
            }

            CommandLine.WriteLine(syncManager.RemoteVault, $"Restoring {restoreFiles.Count:N0} files before {Formatter.FromDateTime(timestamp.ToLocalTime())}...", ConsoleColor.DarkGray);
            _totalFiles += await syncManager.RestoreFilesAsync(restoreFiles, new ProgressReport(syncManager.RemoteVault, restoreFiles.Count));
            CommandLine.WriteLine(syncManager.RemoteVault, $"Successfully restored {_totalFiles:N0} files in {_sw.Elapsed}.", ConsoleColor.Green);
        }
    }
}