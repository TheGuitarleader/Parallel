// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using System.Diagnostics;
using Parallel.Cli.Utils;
using Parallel.Core.Diagnostics;
using Parallel.Core.Settings;

namespace Parallel.Cli.Commands
{
    public class BackupCommand : Command
    {
        private Stopwatch _sw = new Stopwatch();
        private IProgress _progress = new ProgressReport();

        private Command addCmd = new("add", "Adds a new directory to the backup list.");
        private Command listCmd = new("list", "Shows all directories in the backup list.");
        private Command removeCmd = new("remove", "Removes a directory from the backup list.");

        public Argument<string> pathArg = new("path", "The folder path to backup.");
        public Option<string> pathOpt = new(["--path", "-p"], "The folder path to backup.");

        public BackupCommand() : base("backup", "Manages backup folders and files.")
        {
            this.SetHandler(async (path) =>
            {
                _sw = Stopwatch.StartNew();

                ProfileConfig profile = ProfileConfig.Load(Program.Settings, config);
                if (string.IsNullOrEmpty(path))
                {
                    await BackupSystemAsync(profile);
                }
                else
                {
                    await BackupDirectoryAsync(path, profile);
                }
            }, pathOpt);

            this.AddCommand(addCmd);
            addCmd.AddArgument(pathArg);
            addCmd.SetHandler((path, config) =>
            {
                ProfileConfig profile = ProfileConfig.Load(Program.Settings, config);
                if (!profile.BackupDirectories.Add(path))
                {
                    CommandLine.WriteLine($"Failed to add '{path}' to the backup list!", ConsoleColor.Red);
                    return;
                }

                CommandLine.WriteLine($"Successfully added '{path}' to the backup list.", ConsoleColor.Green);
            }, pathArg);

            this.AddCommand(listCmd);
            listCmd.SetHandler(() =>
            {
                ProfileConfig profile = ProfileConfig.Load(Program.Settings, config);
                CommandLine.WriteLine($"Backup list for '{profile.Name}' ({profile.Id}):");
                foreach (string folder in profile.BackupDirectories)
                {
                    CommandLine.WriteLine(folder);
                }
            });

            this.AddCommand(removeCmd);
            removeCmd.AddArgument(pathArg);
            removeCmd.SetHandler((path) =>
            {
                ProfileConfig profile = ProfileConfig.Load(Program.Settings, config);
                if (!profile.BackupDirectories.Remove(path))
                {
                    CommandLine.WriteLine($"Failed to remove '{path}' to the backup list!", ConsoleColor.Red);
                    return;
                }

                CommandLine.WriteLine($"Removed path '{path}' from the backup list.", ConsoleColor.Green);
            }, pathArg);
        }
    }
}