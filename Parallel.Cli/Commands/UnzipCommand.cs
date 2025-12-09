// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using System.Diagnostics;
using System.IO.Compression;
using Parallel.Cli.Utils;

namespace Parallel.Cli.Commands
{
    public class UnzipCommand : Command
    {
        private readonly Argument<string> sourceArg = new("path", "The source path of files to unzip.");
        private readonly Option<bool> keepOpt = new(["--keep", "-k"], "If the original files should be kept.");

        private Stopwatch? _sw;
        private readonly List<Task> _tasks = new List<Task>();
        private int _totalTasks = 0;

        public UnzipCommand() : base("unzip", "Unzips files in a directory.")
        {
            this.AddArgument(sourceArg);
            this.AddOption(keepOpt);
            this.SetHandler(async (path, keep) =>
            {
                _sw = Stopwatch.StartNew();
                CommandLine.WriteLine($"Scanning for files in {path}...", ConsoleColor.DarkGray);
                string[] files = Directory.GetFiles(path, $"*.gz", SearchOption.AllDirectories);
                if (files.Length == 0)
                {
                    CommandLine.WriteLine("No files found to unzip!", ConsoleColor.Yellow);
                    return;
                }

                CommandLine.WriteLine($"Unzipping {files.Length.ToString("N0")} files...", ConsoleColor.DarkGray);
                _totalTasks = files.Length;
                _tasks.AddRange(files.Select(file => Task.Run(() => DecompressFile(file, keep))));
                await Task.WhenAll(_tasks);

                CommandLine.WriteLine($"Successfully unzipped {files.Length.ToString("N0")} files in {_sw.Elapsed}.", ConsoleColor.Green);
            }, sourceArg, keepOpt);
        }

        private void DecompressFile(string path, bool keep)
        {
            if (File.Exists(path))
            {
                using (FileStream openFile = File.OpenRead(path))
                using (FileStream createFile = new FileStream(path.Replace(".gz", string.Empty), FileMode.OpenOrCreate))
                using (GZipStream gZip = new GZipStream(openFile, CompressionMode.Decompress))
                {
                    gZip.CopyTo(createFile);
                }

                if (!keep)
                {
                    File.SetAttributes(path, ~FileAttributes.ReadOnly & File.GetAttributes(path));
                    File.Delete(path);
                }
            }

            CommandLine.ProgressBar(_tasks.Count(t => t.IsCompleted), _totalTasks, _sw?.Elapsed ?? TimeSpan.Zero);
        }
    }
}