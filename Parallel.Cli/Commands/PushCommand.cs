// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using Parallel.Cli.Utils;
using Parallel.Core.IO;
using Parallel.Core.IO.Backup;
using Parallel.Core.IO.Scanning;
using Parallel.Core.Models;

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

        public PushCommand() : base("push", "Pushes files to ")
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
                    if (PathBuilder.IsDirectory(path))
                    {
                        await SyncDirectoryAsync(path);
                    }
                    else if (PathBuilder.IsFile(path))
                    {
                        await SyncFileAsync(path);
                    }
                }
            }, _sourceArg, _configOpt, _verboseOpt);
        }

        private async Task SyncSystemAsync()
        {
            throw new NotImplementedException();
        }

        private async Task SyncDirectoryAsync(string path)
        {
            await Program.Settings.ForEachVaultAsync(async (vault) =>
            {
                ISyncManager sync = SyncManager.CreateNew(vault);
                if (!sync.Initialize())
                {
                    CommandLine.WriteError($"Failed to connect to vault '{vault.Name}'!");
                    return;
                }

                string[] backupFolders = vault.BackupDirectories.ToArray();
                if (!backupFolders.Any(path.StartsWith))
                {
                    CommandLine.WriteWarning($"The provided folder is not set to be backed up!");
                    return;
                }

                string[] ignoredFolders = vault.IgnoreDirectories.ToArray();
                FileScanner scanner = new FileScanner(sync);

                // Checks if the file can be backed up.
                if (FileScanner.IsIgnored(path, ignoredFolders))
                {
                    CommandLine.WriteWarning($"The provided folder is set to be ignored!");
                    return;
                }

                CommandLine.WriteLine($"Scanning for file changes in {path}...", ConsoleColor.DarkGray);
                SystemFile[] files = await scanner.GetFileChangesAsync(path, ignoredFolders);
                int successFiles = files.Length;
                if (successFiles == 0)
                {
                    CommandLine.WriteLine($"The provided directory is already up to date.", ConsoleColor.Green);
                    return;
                }

                CommandLine.WriteLine($"Backing up {files.Length.ToString("N0")} files...", ConsoleColor.DarkGray);
                await sync.PushFilesAsync(files, new ProgressReport());
                CommandLine.WriteLine($"Successfully pushed {successFiles.ToString("N0")} files.", ConsoleColor.Green);
            });
        }

        private async Task SyncFileAsync(string path)
        {
            await Program.Settings.ForEachVaultAsync(async (vault) =>
            {
                ISyncManager sync = SyncManager.CreateNew(vault);
                if (!sync.Initialize())
                {
                    CommandLine.WriteError("Failed to connect to backup file system!");
                    return;
                }

                string[] backupFolders = vault.BackupDirectories.ToArray();
                if (!backupFolders.Any(path.StartsWith))
                {
                    CommandLine.WriteWarning($"The provided file is not set to be backed up!");
                    return;
                }

                string[] ignoredFolders = vault.IgnoreDirectories.ToArray();
                FileScanner scanner = new FileScanner(sync);

                // Checks if the file can be backed up.
                if (FileScanner.IsIgnored(path, ignoredFolders))
                {
                    CommandLine.WriteWarning($"The provided folder is set to be ignored!");
                    return;
                }

                SystemFile localFile = new SystemFile(path);
                SystemFile? remoteFile = await sync.Database.GetFileAsync(localFile.LocalPath);

                if (!FileScanner.HasChanged(localFile, remoteFile))
                {
                    CommandLine.WriteLine($"The provided file is already up to date.", ConsoleColor.Green);
                    return;
                }

                CommandLine.WriteLine($"Pushing: {localFile.LocalPath}", ConsoleColor.DarkGray);
                await sync.PushFilesAsync([localFile], new ProgressReport());
                CommandLine.WriteLine($"Successfully pushed: {localFile.LocalPath}", ConsoleColor.Green);
            });
        }
    }
}