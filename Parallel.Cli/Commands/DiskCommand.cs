// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using System.Data;
using Newtonsoft.Json.Linq;
using Parallel.Cli.Utils;
using Parallel.Core.Database;
using Parallel.Core.IO.Backup;
using Parallel.Core.IO.FileSystem;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Settings;
using Parallel.Core.Utils;

namespace Parallel.Cli.Commands
{
    public class DiskCommand : Command
    {
        private readonly Argument<string> vaultArg = new("vault", "The vault config to use.");

        public DiskCommand() : base("disk", "Shows the current disk usage.")
        {
            this.AddArgument(vaultArg);
            this.SetHandler(async (vault) =>
            {
                CommandLine.WriteLine($"Retrieving disk information...", ConsoleColor.DarkGray);
                LocalVaultConfig? config = ParallelConfig.GetVault(vault);
                if (config == null)
                {
                    CommandLine.WriteLine($"Unable to find vault with name: '{vault}'", ConsoleColor.Yellow);
                    return;
                }

                await DisplayDiskInformationAsync(config);
            }, vaultArg);
        }

        private async Task DisplayDiskInformationAsync(LocalVaultConfig vault)
        {
            ISyncManager syncManager = new FileSyncManager(vault);
            if (!await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, $"Failed to connect to vault '{vault.Name}'!", ConsoleColor.Red);
                return;
            }

            IDatabase db = syncManager.Database;

            long localSize = await db.GetLocalSizeAsync();
            long remoteSize = await db.GetRemoteSizeAsync();
            long totalLocalFiles = await db.GetTotalFilesAsync(false);
            long totalDeletedFiles = await db.GetTotalFilesAsync(true);

            CommandLine.WriteLine($"Using profile '{vault.Name}' ({vault.Id}):");
            CommandLine.WriteLine($"Root Directory: {vault.FileSystem.RootDirectory}");
            CommandLine.WriteLine($"Managed Files:  {(totalLocalFiles + totalDeletedFiles).ToString("N0")}");
            CommandLine.WriteLine($"Local Files:    {totalLocalFiles.ToString("N0")}");
            CommandLine.WriteLine($"Deleted Files:  {totalDeletedFiles.ToString("N0")}");
            CommandLine.WriteLine($"Local Size:     {Formatter.FromBytes(localSize)}");
            CommandLine.WriteLine($"Remote Size:    {Formatter.FromBytes(remoteSize)}");
            CommandLine.WriteLine($"Space Saved:    {Math.Round((localSize - remoteSize) / (double)localSize * 100, 1)}%");

            if (vault.FileSystem.Service.Equals(FileService.Local))
            {
                DriveInfo drive = new(vault.FileSystem.RootDirectory);
                long diskUsage = drive.TotalSize - drive.TotalFreeSpace;
                CommandLine.WriteLine($"Total Usage:    {Formatter.FromBytes(diskUsage)} ({Math.Round(diskUsage / (double)drive.TotalSize * 100, 1)}%)");
                CommandLine.WriteLine($"Disk Usage:     {Formatter.FromBytes(diskUsage - remoteSize)} ({Math.Round((diskUsage - remoteSize) / (double)drive.TotalSize * 100, 1)}%)");
                CommandLine.WriteLine($"Disk Free:      {Formatter.FromBytes(drive.TotalFreeSpace)} ({Math.Round(drive.TotalFreeSpace / (double)drive.TotalSize * 100, 1)}%)");
                CommandLine.WriteLine($"Disk Total:     {Formatter.FromBytes(drive.TotalSize)}");
            }
        }
    }
}