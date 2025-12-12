// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using System.Data;
using Newtonsoft.Json.Linq;
using Parallel.Cli.Utils;
using Parallel.Core.Database;
using Parallel.Core.IO.FileSystem;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Settings;
using Parallel.Core.Utils;

namespace Parallel.Cli.Commands
{
    public class DiskCommand : Command
    {
        private readonly Option<string> _configOpt = new(["--config", "-c"], "The vault configuration to use.");

        public DiskCommand() : base("disk", "Shows the current disk usage.")
        {
            this.AddOption(_configOpt);
            this.SetHandler(async (config) =>
            {
                LocalVaultConfig? vault = ParallelConfig.Load().Vaults.FirstOrDefault();
                if (!string.IsNullOrEmpty(config)) vault = ParallelConfig.GetVault(config);
                if (vault == null)
                {
                    CommandLine.WriteLine($"Unable to find vault with name: '{vault}'", ConsoleColor.Yellow);
                    return;
                }

                await DisplayDiskInformationAsync(vault);
            }, _configOpt);
        }

        private async Task DisplayDiskInformationAsync(LocalVaultConfig vault)
        {
            CommandLine.WriteLine($"Retrieving vault information...", ConsoleColor.DarkGray);
            ISyncManager syncManager = SyncManager.CreateNew(vault);
            if (!await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, $"Failed to connect to vault '{vault.Name}'!", ConsoleColor.Red);
                return;
            }

            IDatabase db = syncManager.Database;
            long localSize = await db.GetLocalSizeAsync();
            long totalLocalFiles = await db.GetTotalFilesAsync(false);
            long totalDeletedFiles = await db.GetTotalFilesAsync(true);
            long totalObjects = await db.GetTotalObjectsAsync();

            CommandLine.WriteLine($"Using vault '{vault.Name}' ({vault.Id}):");
            CommandLine.WriteLine($"Service Type:   {vault.Credentials.Service}");
            CommandLine.WriteLine($"Root Directory: {vault.Credentials.RootDirectory}");
            CommandLine.WriteLine($"Managed Files:  {(totalLocalFiles + totalDeletedFiles):N0}");
            CommandLine.WriteLine($"Local Files:    {totalLocalFiles:N0}");
            CommandLine.WriteLine($"Deleted Files:  {totalDeletedFiles:N0}");
            CommandLine.WriteLine($"Total Objects:  {totalObjects:N0}");
            CommandLine.WriteLine($"Local Size:     {Formatter.FromBytes(localSize)}");

            if (vault.Credentials.Service.Equals(FileService.Local))
            {
                DriveInfo drive = new(vault.Credentials.RootDirectory);
                long diskUsage = drive.TotalSize - drive.TotalFreeSpace;
                CommandLine.WriteLine($"Total Usage:    {Formatter.FromBytes(diskUsage)} ({Math.Round(diskUsage / (double)drive.TotalSize * 100, 1)}%)");
                CommandLine.WriteLine($"Disk Free:      {Formatter.FromBytes(drive.TotalFreeSpace)} ({Math.Round(drive.TotalFreeSpace / (double)drive.TotalSize * 100, 1)}%)");
                CommandLine.WriteLine($"Disk Total:     {Formatter.FromBytes(drive.TotalSize)}");
            }

            await syncManager.DisconnectAsync();
        }
    }
}