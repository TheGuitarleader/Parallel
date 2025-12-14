// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using Parallel.Cli.Utils;
using Parallel.Core.Database;
using Parallel.Core.IO.FileSystem;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Settings;
using Parallel.Core.Utils;

namespace Parallel.Cli.Commands
{
    public class StatsCommand : Command
    {
        private readonly Option<string> _configOpt = new(["--config", "-c"], "The vault configuration to use.");

        public StatsCommand() : base("stats", "Displays various vault statistics.")
        {
            this.AddOption(_configOpt);
            this.SetHandler(async (config) =>
            {
                LocalVaultConfig? vault = ParallelConfig.Load().Vaults.FirstOrDefault(v => v.Enabled);
                if (!string.IsNullOrEmpty(config)) vault = ParallelConfig.GetVault(config);
                if (vault == null)
                {
                    CommandLine.WriteLine($"No vault was found!", ConsoleColor.Yellow);
                    return;
                }

                await DisplayDiskInformationAsync(vault);
            }, _configOpt);
        }

        private async Task DisplayDiskInformationAsync(LocalVaultConfig vault)
        {
            CommandLine.WriteLine($"Retrieving vault information...", ConsoleColor.DarkGray);
            ISyncManager? syncManager = SyncManager.CreateNew(vault);
            if (syncManager == null || !await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, $"Failed to connect to vault '{vault.Name}'!", ConsoleColor.Red);
                return;
            }

            IDatabase db = syncManager.Database;
            long localSize = await db.GetLocalSizeAsync();
            long remoteSize = await db.GetRemoteSizeAsync();
            long totalSize = await db.GetTotalSizeAsync();
            long totalFiles = await db.GetTotalFilesAsync();
            long totalLocalFiles = await db.GetTotalFilesAsync(false);
            long totalDeletedFiles = await db.GetTotalFilesAsync(true);
            double spaceSaved = Math.Round((localSize - remoteSize) / (double)localSize * 100, 2);

            CommandLine.WriteLine($"Using vault '{vault.Name}' ({vault.Id}):");
            CommandLine.WriteLine($"Service Type:   {vault.Credentials.Service}");
            CommandLine.WriteLine($"Root Directory: {vault.Credentials.RootDirectory}");
            CommandLine.WriteLine($"Managed Files:  {totalFiles:N0}");
            CommandLine.WriteLine($"Local Files:    {totalLocalFiles:N0}");
            CommandLine.WriteLine($"Deleted Files:  {totalDeletedFiles:N0}");
            CommandLine.WriteLine($"Total Size:     {Formatter.FromBytes(totalSize)}");
            CommandLine.WriteLine($"Local Size:     {Formatter.FromBytes(localSize)}");
            CommandLine.WriteLine($"Remote Size:    {Formatter.FromBytes(remoteSize)} ({(double.IsNaN(spaceSaved) ? 0 : spaceSaved)}%)");

            if (vault.Credentials.Service.Equals(FileService.Local))
            {
                DriveInfo drive = new(vault.Credentials.RootDirectory);
                long diskUsage = drive.TotalSize - drive.TotalFreeSpace;
                CommandLine.WriteLine($"Total Usage:    {Formatter.FromBytes(diskUsage)} ({Math.Round(diskUsage / (double)drive.TotalSize * 100, 1)}%)");
                CommandLine.WriteLine($"Disk Usage:     {Formatter.FromBytes(diskUsage - remoteSize)} ({Math.Round((diskUsage - remoteSize) / (double)drive.TotalSize * 100, 1)}%)");
                CommandLine.WriteLine($"Disk Free:      {Formatter.FromBytes(drive.TotalFreeSpace)} ({Math.Round(drive.TotalFreeSpace / (double)drive.TotalSize * 100, 1)}%)");
                CommandLine.WriteLine($"Disk Total:     {Formatter.FromBytes(drive.TotalSize)}");
            }

            await syncManager.DisconnectAsync();
        }
    }
}