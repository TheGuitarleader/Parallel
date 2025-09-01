// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using Parallel.Cli.Utils;
using Parallel.Core.IO;
using Parallel.Core.IO.Backup;
using Parallel.Core.IO.Scanning;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Models;
using Parallel.Core.Settings;

namespace Parallel.Cli.Commands
{
    public class PushCommand : Command
    {
        private Command addCmd = new("add", "Adds a new directory to the backup list.");
        private Command listCmd = new("list", "Shows all directories in the backup list.");
        private Command removeCmd = new("remove", "Removes a directory from the backup list.");

        private readonly Option<string> _sourceArg = new(["--path", "-p"], "The source path to backup.");
        private readonly Option<string> _configOpt = new(["--config", "-c"], "The vault configuration to use.");
        private readonly Option<bool> _verboseOpt = new(["--verbose", "-v"], "Shows verbose output.");

        public PushCommand() : base("push", "Pushes changed files to vaults.")
        {
            this.AddOption(_sourceArg);
            this.AddOption(_configOpt);
            this.AddOption(_verboseOpt);
            this.SetHandler(async (path, config, verbose) =>
            {
                if (string.IsNullOrEmpty(path))
                {
                    await SyncSystemAsync();
                }
                else
                {
                    await SyncPathAsync(path);
                }

            }, _sourceArg, _configOpt, _verboseOpt);
        }

        private async Task SyncSystemAsync()
        {
            throw new NotImplementedException();
        }

        private async Task SyncPathAsync(string path)
        {
            await ParallelSettings.ForEachVaultAsync(async vault =>
            {
                ISyncManager sync = SyncManager.CreateNew(vault);
                if (!sync.Initialize())
                {
                    CommandLine.WriteLine(vault, $"Failed to connect to vault '{vault.Name}'!", ConsoleColor.Red);
                    return;
                }

                // Normalize paths for safe comparison
                string fullPath = Path.GetFullPath(path);
                string[] backupFolders = vault.BackupDirectories.ToArray();
                string[] ignoredFolders = vault.IgnoreDirectories.ToArray();

                bool isFile = PathBuilder.IsFile(fullPath);
                if (!backupFolders.Any(dir => fullPath.StartsWith(dir, StringComparison.OrdinalIgnoreCase)))
                {
                    CommandLine.WriteLine(vault, $"The provided {(isFile ? "file" : "folder")} is not set to be backed up!", ConsoleColor.Yellow);
                    return;
                }

                if (FileScanner.IsIgnored(fullPath, ignoredFolders))
                {
                    CommandLine.WriteLine(vault, $"The provided {(isFile ? "file" : "folder")} is set to be ignored!", ConsoleColor.Yellow);
                    return;
                }

                CommandLine.WriteLine(vault, $"Scanning for file changes in {path}...", ConsoleColor.DarkGray);

                FileScanner scanner = new FileScanner(sync);
                SystemFile[] files = await scanner.GetFileChangesAsync(path, ignoredFolders);
                int successFiles = files.Length;
                if (successFiles == 0)
                {
                    CommandLine.WriteLine(vault, $"The provided {(isFile ? "file" : "folder")} is already up to date.", ConsoleColor.Green);
                    return;
                }

                CommandLine.WriteLine(vault, $"Backing up {files.Length.ToString("N0")} files...", ConsoleColor.DarkGray);
                await sync.PushFilesAsync(files, new ProgressReport(vault));
                CommandLine.WriteLine(vault, $"Successfully pushed {successFiles.ToString("N0")} files to '{vault.FileSystem.Address}'.", ConsoleColor.Green);
            });
        }
    }
}