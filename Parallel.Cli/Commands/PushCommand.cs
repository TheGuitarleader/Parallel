// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using System.Diagnostics;
using Parallel.Cli.Utils;
using Parallel.Core.Diagnostics;
using Parallel.Core.IO;
using Parallel.Core.IO.Scanning;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Models;
using Parallel.Core.Settings;

namespace Parallel.Cli.Commands
{
    public class PushCommand : Command
    {
        private Stopwatch _sw = new Stopwatch();

        private Command addCmd = new("add", "Adds a new directory to the sync list.");
        private Command listCmd = new("list", "Shows all directories in the sync list.");
        private Command removeCmd = new("remove", "Removes a directory from the sync list.");

        private readonly Option<string> _sourceArg = new(["--path", "-p"], "The source path to sync.");
        private readonly Option<string> _configOpt = new(["--config", "-c"], "The vault configuration to use.");
        private readonly Option<bool> _forceOpt = new(["--force", "-f"], "Forces the push overwriting any files.");

        public PushCommand() : base("push", "Pushes changed files to vaults.")
        {
            this.AddOption(_sourceArg);
            this.AddOption(_configOpt);
            this.AddOption(_forceOpt);
            this.SetHandler(async (path, config, force) =>
            {
                _sw = Stopwatch.StartNew();
                if (string.IsNullOrEmpty(path))
                {
                    await SyncSystemAsync();
                }
                else
                {
                    await SyncPathAsync(path, force);
                }

            }, _sourceArg, _configOpt, _forceOpt);
        }

        private Task SyncSystemAsync()
        {
            throw new NotImplementedException();
        }

        private async Task SyncPathAsync(string path, bool force)
        {
            await Program.Settings.ForEachVaultAsync(async vault =>
            {
                ISyncManager? syncManager = SyncManager.CreateNew(vault);
                if (syncManager == null || !await syncManager.ConnectAsync())
                {
                    CommandLine.WriteLine(vault, $"Failed to connect to vault '{vault.Name}'!", ConsoleColor.Red);
                    return;
                }

                // Normalize paths for safe comparison
                string fullPath = Path.GetFullPath(path);
                string[] backupFolders = syncManager.RemoteVault.BackupDirectories.ToArray();
                string[] ignoredFolders = syncManager.RemoteVault.IgnoreDirectories.ToArray();

                bool isFile = PathBuilder.IsFile(fullPath);
                if (!PathBuilder.IsDirectory(fullPath) && !isFile)
                {
                    CommandLine.WriteLine(vault, $"The provided path is invalid!", ConsoleColor.Yellow);
                    return;
                }

                if (!backupFolders.Any(dir => fullPath.StartsWith(dir, StringComparison.OrdinalIgnoreCase)) || FileScanner.IsIgnored(fullPath, ignoredFolders))
                {
                    CommandLine.WriteLine(vault, $"The provided {(isFile ? "file" : "folder")} is set to be ignored!", ConsoleColor.Yellow);
                    return;
                }

                CommandLine.WriteLine(vault, $"Scanning for file changes in {path}...", ConsoleColor.DarkGray);
                FileScanner scanner = new FileScanner(syncManager);
                SystemFile[] files = await scanner.GetFileChangesAsync(path, ignoredFolders, force);
                int successFiles = files.Length;
                if (successFiles == 0)
                {
                    CommandLine.WriteLine(vault, $"The provided {(isFile ? "file" : "folder")} is already up to date.", ConsoleColor.Green);
                    return;
                }

                CommandLine.WriteLine(vault, $"Backing up {files.Length:N0} files...", ConsoleColor.DarkGray);
                int pushedFiles = await syncManager.PushFilesAsync(files, new ProgressReport(vault, successFiles));
                await syncManager.DisconnectAsync();

                CommandLine.WriteLine(vault, $"Successfully pushed {pushedFiles:N0} files in {_sw.Elapsed}.", ConsoleColor.Green);
            });
        }
    }
}