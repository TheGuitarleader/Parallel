// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using Parallel.Cli.Utils;
using Parallel.Core.IO;
using Parallel.Core.IO.Scanning;
using Parallel.Core.Models;
using Parallel.Core.Settings;
using Parallel.Core.Utils;
using TextWriter = Parallel.Cli.Utils.TextWriter;

namespace Parallel.Cli.Commands
{
    public class DuplicatesCommand : Command
    {
        private readonly Argument<string> sourceArg = new("path", "The directory to scan.");

        public DuplicatesCommand() : base("duplicates", "Scans a directory for duplicate files.")
        {
            this.AddArgument(sourceArg);
            this.SetHandler(async (path) =>
            {
                await ScanForDuplicateFiles(path);
            }, sourceArg);
        }

        private async Task ScanForDuplicateFiles(string path)
        {
            CommandLine.WriteLine($"Scanning for duplicate files in {path}...", ConsoleColor.DarkGray);
            Dictionary<string, SystemFile[]> duplicates = FileScanner.GetDuplicateFiles(path);
            Dictionary<string, string[]> result = duplicates.ToDictionary(k => k.Key, v => v.Value.Select(l => l.LocalPath).ToArray());
            long length = duplicates.Sum(kv => kv.Value.Sum(l => l.LocalSize));

            List<string> lines = new List<string>();
            foreach (string key in result.Keys)
            {
                string[] paths = result[key];
                lines.Add($"{key} ({paths.Length:N0} items):");
                lines.AddRange(paths.Select(value => $" - {value}"));
            }

            CommandLine.WriteLine($"Opening editor...", ConsoleColor.DarkGray);
            string fileName = Path.Combine(PathBuilder.TempDirectory, DateTime.Now.ToString("MM-dd-yyyy hh-mm-ss") + ".txt");
            await File.WriteAllLinesAsync(fileName, lines);
            CommandLine.OpenFile(fileName, false);

            CommandLine.WriteLine($"Scan found {duplicates.Count(kv => kv.Value.Length > 1):N0} duplicate files. ({Formatter.FromBytes(length)})");
        }
    }
}