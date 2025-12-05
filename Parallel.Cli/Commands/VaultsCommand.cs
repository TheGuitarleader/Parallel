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

        private Command addCmd = new("add", "Adds a new vault configuration.");
        private Command editCmd = new("edit", "Edits a vault configuration.");
        private Command viewCmd = new("view", "Shows the vault configuration.");
        private Command setCmd = new("set", "Sets a new vault configuration.");
        private Command delCmd = new("delete", "Deletes a vault configuration.");

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
                FileSystemCredentials fsc = new FileSystemCredentials();
                fsc.Service = Enum.Parse<FileService>(CommandLine.ReadString($"Service ({string.Join(", ", Enum.GetNames(typeof(FileService)))})"), true);
                if (fsc.Service == FileService.Local)
                {
                    fsc.RootDirectory = CommandLine.ReadString("Root");
                }
                else if (fsc.Service == FileService.Cloud)
                {
                    fsc.Address = CommandLine.ReadString("Bucket Name");
                    fsc.Username = CommandLine.ReadString("Access Key");
                    fsc.Password = CommandLine.ReadPassword("Secret Key");
                }
                else
                {
                    fsc.RootDirectory = CommandLine.ReadString("Root");
                    fsc.Address = CommandLine.ReadString("Address");
                    fsc.Username = CommandLine.ReadString("Username");
                    fsc.Password = CommandLine.ReadPassword("Password");
                }

                //fsc.Encrypt = CommandLine.ReadBool("Encrypt files? (y/n)", false);
                //fsc.EncryptionKey = HashGenerator.GenerateHash(32, true);

                string? profileName = CommandLine.ReadString("Profile Name");
                LocalVaultConfig localVault = new LocalVaultConfig(profileName, fsc);
                Program.Settings.Vaults.Add(localVault);
                Program.Settings.Save();

                CommandLine.WriteLine($"Saved new storage vault: '{localVault.Name}' ({localVault.Id})");
            });

            this.AddCommand(viewCmd);
            viewCmd.AddArgument(configArg);
            viewCmd.SetHandler(async (vault) =>
            {
                CommandLine.WriteLine($"Retrieving vault information...", ConsoleColor.DarkGray);
                LocalVaultConfig? config = ParallelConfig.GetVault(vault);
                if (config == null)
                {
                    CommandLine.WriteLine($"Unable to find vault with name: '{vault}'", ConsoleColor.Yellow);
                    return;
                }

                ISyncManager syncManager = new FileSyncManager(config);
                if (!await syncManager.ConnectAsync())
                {
                    CommandLine.WriteLine(config, $"Failed to connect to vault '{config.Name}'!", ConsoleColor.Red);
                    return;
                }

                RemoteVaultConfig remoteVault = syncManager.RemoteVault;
                CommandLine.WriteLine($"'{remoteVault.Name}' ({remoteVault.Id}):");
                CommandLine.WriteArray($"Backup Directories", remoteVault.BackupDirectories);
                CommandLine.WriteArray($"Ignore Directories", remoteVault.IgnoreDirectories);
                CommandLine.WriteArray($"Prune Directories", remoteVault.PruneDirectories);
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