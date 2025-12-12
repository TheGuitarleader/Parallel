// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using Parallel.Cli.Utils;
using Parallel.Core.Database;
using Parallel.Core.IO.FileSystem;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Security;
using Parallel.Core.Settings;
using Parallel.Core.Utils;

namespace Parallel.Cli.Commands
{
    public class VaultsCommand : Command
    {
        private readonly Argument<string> configArg = new("config", "The vault configuration to use.");
        private readonly Option<string> configOpt = new(["--config", "-c"], "The vault configuration to use.");

        private readonly Command addCmd = new("add", "Adds a new vault configuration.");
        private readonly Command editCmd = new("edit", "Edits a vault configuration.");
        private readonly Command findCmd = new("find", "Finds vault configurations in a location.");
        private readonly Command viewCmd = new("view", "Shows the vault configuration.");
        private readonly Command setCmd = new("set", "Sets a new vault configuration.");
        private readonly Command delCmd = new("delete", "Deletes a vault configuration.");

        public VaultsCommand() : base("vaults", "View or edit the vaults.")
        {
            this.SetHandler(() =>
            {
                CommandLine.WriteLine("Active vaults:");
                for (int i = 0; i < Program.Settings.Vaults.Count; i++)
                {
                    LocalVaultConfig vault = Program.Settings.Vaults.ElementAt(i);
                    CommandLine.WriteLine($"{i + 1}: {vault.Name} ({vault.Id})");
                }
            });

            this.AddCommand(addCmd);
            addCmd.SetHandler(() =>
            {
                CommandLine.WriteLine("Creating new storage vault...", ConsoleColor.DarkGray);
                StorageCredentials spc = new StorageCredentials
                {
                    Service = Enum.Parse<FileService>(CommandLine.ReadString($"Service ({string.Join(", ", Enum.GetNames(typeof(FileService)))})") ?? string.Empty, true)
                };

                if (spc.Service == FileService.Local)
                {
                    CommandLine.WriteLine("It is NOT RECOMMENDED to use a network drive!", ConsoleColor.Yellow);
                    spc.RootDirectory = CommandLine.ReadString("Root") ?? string.Empty;
                }
                else if (spc.Service == FileService.Cloud)
                {
                    spc.Address = CommandLine.ReadString("Bucket Name");
                    spc.Username = CommandLine.ReadString("Access Key");
                    spc.Password = CommandLine.ReadPassword("Secret Key");
                }
                else
                {
                    spc.RootDirectory = CommandLine.ReadString("Root") ?? string.Empty;
                    spc.Address = CommandLine.ReadString("Address");
                    spc.Username = CommandLine.ReadString("Username");
                    spc.Password = CommandLine.ReadPassword("Password");
                }

                string profileId = CommandLine.ReadString("Id") ?? HashGenerator.GenerateHash(8, true);
                string profileName = CommandLine.ReadString("Name") ?? "Default";

                spc.Encrypt = CommandLine.ReadBool("Encrypt files? (y/n)", false);
                spc.EncryptionKey = spc.Encrypt ? HashGenerator.GenerateHash(32, true) : null;

                LocalVaultConfig localVault = new(profileId, profileName, spc);
                Program.Settings.Vaults.Add(localVault);
                Program.Settings.Save();

                CommandLine.WriteLine($"Saved new storage vault: '{localVault.Name}' ({localVault.Id})");
            });

            this.AddCommand(findCmd);
            findCmd.SetHandler(() =>
            {

            });

            this.AddCommand(viewCmd);
            viewCmd.AddArgument(configArg);
            viewCmd.SetHandler(async (config) =>
            {
                CommandLine.WriteLine($"Retrieving vault information...", ConsoleColor.DarkGray);
                LocalVaultConfig? vault = ParallelConfig.GetVault(config);
                if (vault == null)
                {
                    CommandLine.WriteLine($"Unable to find vault with name: '{vault}'", ConsoleColor.Yellow);
                    return;
                }

                ISyncManager? syncManager = SyncManager.CreateNew(vault);
                if (syncManager == null || !await syncManager.ConnectAsync())
                {
                    CommandLine.WriteLine(vault, $"Failed to connect to vault '{vault.Name}'!", ConsoleColor.Red);
                    return;
                }

                RemoteVaultConfig remoteVault = syncManager.RemoteVault;
                CommandLine.WriteLine($"'{remoteVault.Name}' ({remoteVault.Id}):");
                CommandLine.WriteArray("Backup Directories", remoteVault.BackupDirectories);
                CommandLine.WriteArray("Ignore Directories", remoteVault.IgnoreDirectories);
                CommandLine.WriteArray("Prune Directories", remoteVault.PruneDirectories);
                CommandLine.WriteLine($"Prune Period: {remoteVault.PrunePeriod} days");

                await syncManager.DisconnectAsync();
            }, configArg);

            this.AddCommand(setCmd);
            setCmd.SetHandler(() =>
            {

            });
        }
    }
}