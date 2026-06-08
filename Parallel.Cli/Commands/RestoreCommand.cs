// Copyright 2026 Kyle Ebbinga

using System.Collections.Concurrent;
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
        private readonly Option<bool> _forceOpt = new(["--force", "-f"], "Forces overwriting any files.");
        private readonly Option<bool> _dryRunOpt = new(["--dry-run"], "Previews a command without executing it.");

        public RestoreCommand() : base("restore", "Restores files from the backup.")
        {
            this.AddOption(_sourceOpt);
            this.AddOption(_configOpt);
            this.AddOption(_beforeOpt);
            this.AddOption(_remapOpt);
            this.AddOption(_forceOpt);
            this.AddOption(_dryRunOpt);
            this.SetHandler(HandleRestoreAsync, _sourceOpt, _configOpt, _beforeOpt, _remapOpt, _forceOpt, _dryRunOpt);
        }

        private async Task HandleRestoreAsync(string? path, string? config, DateTime before, string? remap, bool force, bool dryRun)
        {
            _sw = Stopwatch.StartNew();
            DateTime timestamp = before != DateTime.MinValue ? before.AddMinutes(1).AddTicks(-1) : DateTime.Now;
            LocalVaultConfig? localVault = ParallelConfig.GetVault(config);
            if (localVault != null)
            {
                if (!string.IsNullOrEmpty(path))
                {
                    await RestorePathAsync(localVault, path, timestamp, remap, force, dryRun);
                }
                else
                {
                    await RestoreSystemAsync(localVault, timestamp, remap, force, dryRun);
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(path))
                {
                    await Program.Settings.ForEachVaultAsync(vault => RestorePathAsync(vault, path, timestamp, remap, force, dryRun));
                }
                else
                {
                    await Program.Settings.ForEachVaultAsync(vault => RestoreSystemAsync(vault, timestamp, remap, force, dryRun));
                }
            }
        }

        private async Task RestoreSystemAsync(LocalVaultConfig vault, DateTime timestamp, string? output, bool force, bool dryRun)
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
                    await RestoreInternalAsync(syncManager, path, timestamp, output, force, dryRun);
                }
            }
            finally
            {
                await syncManager.DisconnectAsync();
            }
        }

        private async Task RestorePathAsync(LocalVaultConfig vault, string path, DateTime timestamp, string? output, bool force, bool dryRun)
        {
            ISyncManager? syncManager = SyncManager.CreateNew(vault);
            if (syncManager == null || !await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, "Failed to connect to vault!", ConsoleColor.Red);
                return;
            }

            await RestoreInternalAsync(syncManager, path, timestamp, output, force, dryRun);
        }

        private async Task RestoreInternalAsync(ISyncManager syncManager, string path, DateTime timestamp, string? output, bool force, bool dryRun)
        {
            CommandLine.WriteLine(syncManager.RemoteVault, $"Scanning for files in {path}...", ConsoleColor.DarkGray);
            IReadOnlyList<LocalFile> files = await (syncManager.Database?.GetLatestFilesAsync(path, timestamp) ?? Task.FromResult<IReadOnlyList<LocalFile>>([]));
            Log.Debug($"GetLatestFilesAsync returned {files.Count} files for path '{path}'");

            ConcurrentBag<LocalFile> restoreFiles = new();
            System.Threading.Tasks.Parallel.ForEach(files, ParallelConfig.Options, (file) =>
            {
                string sourcePath = file.Fullname;
                //string outputPath = string.IsNullOrEmpty(output) ? sourcePath : PathBuilder.ReplacePath(sourcePath, path, output);
                string outputPath = PathBuilder.ReplacePath(file.Fullname, path, output);
                if (File.Exists(outputPath) && !FileScanner.HasChanged(file, new LocalFile(outputPath)) && !force) return;
                
                file.Fullname = outputPath;
                restoreFiles.Add(file);
            });

            if (restoreFiles.Count == 0)
            {
                CommandLine.WriteLine(syncManager.RemoteVault, $"The provided {(PathBuilder.IsFile(path) ? "file" : "folder")} is already up to date.", ConsoleColor.Green);
                return;
            }

            if (dryRun)
            {
                string fileName = PathBuilder.TempFile;
                await File.WriteAllLinesAsync(fileName, restoreFiles.Select(f => f.Fullname).OrderBy(f => f));
                CommandLine.WriteLine($"This operation will restore {restoreFiles.Count:N0} files into: {(string.IsNullOrEmpty(output) ? path : output)}", ConsoleColor.Green);
                CommandLine.WriteLine($"A detailed list can be found here: {fileName}", ConsoleColor.DarkGray);
            }
            else
            {
                CommandLine.WriteLine(syncManager.RemoteVault, $"Restoring {restoreFiles.Count:N0} files...", ConsoleColor.DarkGray);
                _totalFiles += await syncManager.RestoreFilesAsync(restoreFiles.ToArray(), new ProgressReport(syncManager.RemoteVault, restoreFiles.Count));
                CommandLine.WriteLine(syncManager.RemoteVault, $"Successfully restored {_totalFiles:N0} files in {_sw.Elapsed}.", ConsoleColor.Green);   
                await syncManager.DisconnectAsync();
            }
        }
    }
}