// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using Parallel.Cli.Utils;
using Parallel.Core.IO.Backup;
using Parallel.Core.IO.Scanning;
using Parallel.Core.Models;
using Parallel.Core.Settings;

using TextWriter = Parallel.Cli.Utils.TextWriter;

namespace Parallel.Cli.Commands
{
    public class DuplicatesCommand : Command
    {
        private Argument<string> sourceArg = new("path", "The directory to scan.");
        private Option<string> credsOpt = new(["--credentials", "-c"], "The file system credentials to use.");

        public DuplicatesCommand() : base("duplicates", "Scans a directory for duplicate files.")
        {
            this.AddArgument(sourceArg);
            this.AddOption(credsOpt);
            this.SetHandler((path, config) =>
            {
                ProfileConfig profile = ProfileConfig.Load(Program.Settings, config);
                ScanForDuplicateFiles(path, profile);
            }, sourceArg, credsOpt);
        }

        private void ScanForDuplicateFiles(string path, ProfileConfig profile)
        {
            IBackupManager backup = BackupManager.CreateNew(profile);
            if (!backup.Initialize())
            {
                CommandLine.WriteLine("Failed to connect to backup file system!", ConsoleColor.Red);
                return;
            }

            if (!Directory.Exists(path))
            {
                CommandLine.WriteLine("The provided directory is invalid!", ConsoleColor.Yellow);
                return;
            }

            CommandLine.WriteLine($"Scanning for duplicate files in {path}...", ConsoleColor.DarkGray);
            Dictionary<string, SystemFile[]> duplicates = FileScanner.GetDuplicateFiles(path);
            Dictionary<string, string[]> result = duplicates.ToDictionary(k => k.Key, v => v.Value.Select(l => l.LocalPath).ToArray());
            long length = duplicates.Sum(kv => kv.Value.Sum(l => l.LocalSize));

            CommandLine.WriteLine($"Scan found {duplicates.Where(kv => kv.Value.Length > 1).Count().ToString("N0")} duplicate files. ({Formatter.FromBytes(length)})");
            CommandLine.WriteLine($"A detailed version was created here: {TextWriter.CreateTxtFile(JsonConvert.SerializeObject(result, Formatting.Indented))}");
        }
    }
}