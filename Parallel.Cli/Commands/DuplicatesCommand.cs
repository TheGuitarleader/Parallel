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

        public DuplicatesCommand() : base("duplicates", "Scans a directory for duplicate files.")
        {
            this.AddArgument(sourceArg);
            this.SetHandler(ScanForDuplicateFiles, sourceArg);
        }

        private void ScanForDuplicateFiles(string path)
        {
            CommandLine.WriteLine($"Scanning for duplicate files in {path}...", ConsoleColor.DarkGray);
            Dictionary<string, SystemFile[]> duplicates = FileScanner.GetDuplicateFiles(path);
            Dictionary<string, string[]> result = duplicates.ToDictionary(k => k.Key, v => v.Value.Select(l => l.LocalPath).ToArray());
            long length = duplicates.Sum(kv => kv.Value.Sum(l => l.LocalSize));

            CommandLine.WriteLine($"Scan found {duplicates.Where(kv => kv.Value.Length > 1).Count().ToString("N0")} duplicate files. ({Formatter.FromBytes(length)})");
            CommandLine.WriteLine($"A detailed version was created here: {TextWriter.CreateTxtFile(JsonConvert.SerializeObject(result, Formatting.Indented))}");
        }
    }
}