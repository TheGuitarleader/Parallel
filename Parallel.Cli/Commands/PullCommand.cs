// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using Newtonsoft.Json.Linq;
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
                LocalVaultConfig? vault = ParallelConfig.Load().Vaults.FirstOrDefault(v => v.Enabled);
                if (!string.IsNullOrEmpty(config)) vault = ParallelConfig.GetVault(config);
                if (vault == null)
                {
                    CommandLine.WriteLine($"No vault was found!", ConsoleColor.Yellow);
                    return;
                }

                await PullPathAsync(vault, path, force);
            }, _sourceArg, _configOpt, _forceOpt);
        }

        private async Task PullPathAsync(LocalVaultConfig vault, string path, bool force)
        {
            CommandLine.WriteLine($"Retrieving vault information...", ConsoleColor.DarkGray);
            ISyncManager? syncManager = SyncManager.CreateNew(vault);
            if (syncManager == null || !await syncManager.ConnectAsync())
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

            CommandLine.WriteLine(vault, $"Scanning for files in {path}...", ConsoleColor.DarkGray);
            IEnumerable<SystemFile> files = await syncManager.Database.GetFilesAsync(fullPath);
            if (!files.Any())
            {
                CommandLine.WriteLine(vault, "No files were found!", ConsoleColor.Yellow);
                await syncManager.DisconnectAsync();
                return;
            }

            List<SystemFile> pullFiles = new List<SystemFile>();
            System.Threading.Tasks.Parallel.ForEach(files, ParallelConfig.Options, (file) =>
            {
                if (!File.Exists(file.LocalPath) || FileScanner.HasChanged(file, new SystemFile(file.LocalPath)) || force) pullFiles.Add(file);
            });

            Log.Debug($"Pulling {pullFiles.Count} files...");
            await syncManager.PullFilesAsync(pullFiles.ToArray(), new ProgressReport(vault, files.Count()));
            CommandLine.WriteLine(vault, $"Successfully pulled {pullFiles.Count:N0} files from '{vault.Credentials.RootDirectory}'.", ConsoleColor.Green);
        }

        private async Task PullFileAsync(ISyncManager syncManager, string fullPath, bool force)
        {
            SystemFile? remoteFile = await syncManager.Database.GetFileAsync(fullPath);
            if (remoteFile == null)
            {
                CommandLine.WriteLine("The provided file was not found!", ConsoleColor.Yellow);
                await syncManager.DisconnectAsync();
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
            await syncManager.DisconnectAsync();

            CommandLine.WriteLine(syncManager.RemoteVault, $"Successfully pulled file from '{syncManager.RemoteVault.Credentials.RootDirectory}'.", ConsoleColor.Green);
        }
    }
}