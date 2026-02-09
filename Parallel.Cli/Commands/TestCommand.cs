// Copyright 2026 Kyle Ebbinga

using System.CommandLine;
using Parallel.Cli.Utils;
using Parallel.Core.Database.Contexts;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Models;
using Parallel.Core.Settings;

namespace Parallel.Cli.Commands
{
    public class TestCommand : Command
    {
        public TestCommand() : base("test", "Command for testing features.")
        {
            this.SetHandler(async () =>
            {
                LocalVaultConfig? localVault = ParallelConfig.GetVault("a7f9c2eq");
                if (localVault == null) return;
                
                ISyncManager? syncManager = SyncManager.CreateNew(localVault);
                if (syncManager == null || !await syncManager.ConnectAsync())
                {
                    CommandLine.WriteLine(localVault, "Failed to connect to vault!", ConsoleColor.Red);
                    return;
                }

                string filePath = @"C:\Users\kebbi\AppData\Local\Temp\Parallel\a7f9c2eq.db";
                SemaphoreContext context = new($"Data Source={filePath};Cache=Shared;Mode=ReadWriteCreate;Pooling=false;");
                
                IReadOnlyList<SystemFile> files = await context.QueryAsync<SystemFile>("SELECT * FROM objects WHERE parentdir IS NULL;");
                foreach (SystemFile file in files)
                {
                    Console.WriteLine($"Updating {file.LocalPath}...");
                    file.ParentDirectory = Path.GetDirectoryName(file.LocalPath);
                    await syncManager.Database?.AddFileAsync(file);
                }

                await syncManager.DisconnectAsync();
            });
        }
    }
}