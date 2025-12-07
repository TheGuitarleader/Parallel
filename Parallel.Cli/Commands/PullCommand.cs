// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using Parallel.Cli.Utils;
using Parallel.Core.Diagnostics;
using Parallel.Core.IO;
using Parallel.Core.IO.Scanning;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Models;
using Parallel.Core.Settings;

namespace Parallel.Cli.Commands
{
    public class PullCommand : Command
    {
        private readonly Option<string> _sourceArg = new(["--path", "-p"], "The source path to sync.");
        private readonly Option<string> _configOpt = new(["--config", "-c"], "The vault configuration to use.");
        private readonly Option<bool> _forceOpt = new(["--force", "-f"], "Forces the pull overwriting any files.");

        public PullCommand() : base("pull", "Pulls changes from a vault.")
        {
            this.AddOption(_sourceArg);
            this.AddOption(_configOpt);
            this.AddOption(_forceOpt);
            this.SetHandler(async (path, config, force) =>
            {
                LocalVaultConfig? vault = ParallelConfig.GetVault(config);
                if (vault == null)
                {
                    CommandLine.WriteLine($"Unable to find vault with name: '{vault}'", ConsoleColor.Yellow);
                    return;
                }

                await PullPathAsync(vault, path, force);
            }, _sourceArg, _configOpt, _forceOpt);
        }

        private async Task PullPathAsync(LocalVaultConfig vault, string path, bool force)
        {
            ISyncManager syncManager = SyncManager.CreateNew(vault);
            if (!await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, $"Failed to connect to vault '{vault.Name}'!", ConsoleColor.Red);
                return;
            }

            string fullPath = Path.GetFullPath(path);
            if (PathBuilder.IsFile(fullPath))
            {
                await PullFileAsync(syncManager, fullPath, force);
                return;
            }

            IEnumerable<SystemFile> files = await syncManager.Database.GetFilesAsync(fullPath);
            if (!files.Any())
            {
                CommandLine.WriteLine("The provided directory has not been pushed!", ConsoleColor.Yellow);
                return;
            }

            List<SystemFile> pullFiles = new List<SystemFile>();
            await System.Threading.Tasks.Parallel.ForEachAsync(files, ParallelConfig.Options, async (file, ct) =>
            {
                if (!File.Exists(file.LocalPath) || FileScanner.HasChanged(file, new SystemFile(file.LocalPath)) || force) pullFiles.Add(file);
            });

            Log.Debug($"Pulling {pullFiles.Count} files...");
            await syncManager.PullFilesAsync(pullFiles.ToArray(), new ProgressLogger());
            CommandLine.WriteLine(vault, $"Successfully pulled {pullFiles.Count:N0} files from '{vault.FileSystem.RootDirectory}'.", ConsoleColor.Green);
        }

        private async Task PullFileAsync(ISyncManager syncManager, string fullPath, bool force)
        {
            SystemFile? remoteFile = await syncManager.Database.GetFileAsync(fullPath);
            if (remoteFile == null)
            {
                CommandLine.WriteLine("The provided file has not been pushed!", ConsoleColor.Yellow);
                return;
            }

            SystemFile localFile = new SystemFile(fullPath);
            if (!(FileScanner.HasChanged(localFile, remoteFile) || force))
            {
                CommandLine.WriteLine("Cannot overwrite an existing file!", ConsoleColor.Yellow);
                return;
            }

            Log.Debug($"Pulling '{fullPath}'");
            await syncManager.PullFilesAsync([remoteFile], new ProgressLogger());
            CommandLine.WriteLine(syncManager.RemoteVault, $"Successfully pulled file from '{syncManager.RemoteVault.FileSystem.RootDirectory}'.", ConsoleColor.Green);
        }
    }
}