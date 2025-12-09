// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics;
using Parallel.Cli.Utils;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Models;
using Parallel.Core.Security;
using Parallel.Core.Settings;
using SQLitePCL;

namespace Parallel.Cli.Commands
{
    public class RemapCommand : Command
    {
        private readonly Argument<string> _sourceArg = new("source", "The source path to change.");
        private readonly Argument<string> _targetArg = new("target", "The target path to change to.");
        private readonly Option<string> _optionOpt = new("config", "The vault configuration to use.");

        private Stopwatch _sw = new Stopwatch();

        public RemapCommand() : base("remap", "Remaps paths in the vault.")
        {
            this.AddArgument(_sourceArg);
            this.AddArgument(_targetArg);
            this.SetHandler(async (config, source, target) =>
            {
                _sw = Stopwatch.StartNew();
                LocalVaultConfig? vault = ParallelConfig.Load().Vaults.FirstOrDefault();
                if (!string.IsNullOrEmpty(config)) vault = ParallelConfig.GetVault(config);
                if (vault == null)
                {
                    CommandLine.WriteLine($"Unable to find vault with name: '{vault}'", ConsoleColor.Yellow);
                    return;
                }

                if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
                {
                    CommandLine.WriteLine("The source and target paths must be specified!", ConsoleColor.Yellow);
                    return;
                }

                await RemapPathAsync(vault, source, target);
            }, _optionOpt, _sourceArg, _targetArg);

        }

        private async Task RemapPathAsync(LocalVaultConfig vault, string source, string target)
        {
            ISyncManager syncManager = SyncManager.CreateNew(vault);
            if (!await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, $"Failed to connect to vault '{vault.Name}'!", ConsoleColor.Red);
                return;
            }

            CommandLine.WriteLine(vault, $"Scanning for files in {source}...", ConsoleColor.DarkGray);
            IEnumerable<SystemFile> files = await syncManager.Database.GetFilesAsync(source);
            if (!files.Any())
            {
                CommandLine.WriteLine(vault, "No files were found!", ConsoleColor.Yellow);
                return;
            }

            int progress = 0;
            int total = files.Count();

            CommandLine.WriteLine(vault, $"Remapping '{source}' to '{target}'...");
            await System.Threading.Tasks.Parallel.ForEachAsync(files, async (file, ct) =>
            {
                CommandLine.ProgressBar(progress++, total, _sw.Elapsed);

                string newPath = file.LocalPath.Replace(source, target);
                string newId = HashGenerator.CreateSHA1(newPath);
                await syncManager.Database.RemapObjectsAsync(file.Id, newId);
                await syncManager.Database.RemoveFileAsync(file);

                file.Id = newId;
                file.LocalPath = newPath;
                await syncManager.Database.AddFileAsync(file);
            });

            await syncManager.DisconnectAsync();
            CommandLine.WriteLine(vault, $"Successfully remapped {files.Count():N0} files to '{target}'.", ConsoleColor.Green);
        }
    }
}