// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using Parallel.Cli.Utils;
using Parallel.Core.Database;
using Parallel.Core.IO.FileSystem;
using Parallel.Core.Settings;
using Parallel.Core.Utils;

namespace Parallel.Cli.Commands
{
    public class ConfigCommand : Command
    {
        private Option<string> configOpt = new(["--config", "-c"], "The profile configuration to use.");

        private Command addCmd = new("add", "Adds a new profile configuration.");
        private Command editCmd = new("edit", "Edits a profile configuration.");
        private Command viewCmd = new("view", "Shows the profile configuration.");
        private Command setCmd = new("set", "Sets a new profile configuration.");
        private Command delCmd = new("delete", "Deletes a profile configuration.");

        public ConfigCommand() : base("config", "View or edit the profile configurations.")
        {
            this.SetHandler(() =>
            {
                //ProfileConfig profile = ProfileConfig.Load();
                CommandLine.WriteLine($"Current profile: '{Program.Settings.Profiles.FirstOrDefault()}'");
            });

            this.AddCommand(addCmd);
            addCmd.SetHandler(() =>
            {
                CommandLine.WriteLine("Creating new database credentials...", ConsoleColor.DarkGray);
                DatabaseCredentials dbc = new DatabaseCredentials();
                dbc.Provider = Enum.Parse<DatabaseProvider>(CommandLine.ReadString($"Provider ({string.Join(", ", Enum.GetNames(typeof(DatabaseProvider)))})"), true);
                if (dbc.Provider == DatabaseProvider.Local)
                {
                    dbc = DatabaseCredentials.Local;
                }
                else
                {
                    dbc.Address = CommandLine.ReadString("Address");
                    dbc.Username = CommandLine.ReadString("Username");
                    dbc.Password = CommandLine.ReadPassword("Password");
                    dbc.Name = CommandLine.ReadString("Name");
                }

                CommandLine.WriteLine("Creating new file system credentials...", ConsoleColor.DarkGray);
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

                fsc.Encrypt = CommandLine.ReadBool("Encrypt files? (y/n)", false);
                fsc.EncryptionKey = HashGenerator.GenerateHash(32, true);

                string profileName = CommandLine.ReadString("Profile Name");
                ProfileConfig profile = new ProfileConfig(profileName, dbc, fsc);
                profile.SaveToFile();

                CommandLine.WriteLine($"Saved new connection profile: '{profile.Name}'");
            });

            this.AddCommand(setCmd);
            setCmd.SetHandler(() =>
            {

            });
        }
    }
}