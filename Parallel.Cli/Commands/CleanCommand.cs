// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using Parallel.Cli.Utils;
using Parallel.Core.IO.Backup;
using Parallel.Core.IO.Scanning;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Settings;
using Parallel.Core.Utils;

namespace Parallel.Cli.Commands
{
    public class CleanCommand : Command
    {
        private long _freedBytes = 0;

        private readonly Option<string> _sourceOpt = new(["--path", "-p"], "The source path to clean.");
        private readonly Option<int> _daysOpt = new(["--days", "-d"], "The amount of days to hang onto files.");
        private readonly Option<bool> _recursiveOpt = new(["--recursive", "-R"], "If to include subdirectories.");
        private readonly Option<bool> _verboseOpt = new(["--verbose", "-v"], "Shows verbose output.");

        public CleanCommand() : base("clean", "Cleans up the file system by removing old files.")
        {
            this.AddOption(_sourceOpt);
            this.AddOption(_daysOpt);
            this.AddOption(_recursiveOpt);
            this.AddOption(_verboseOpt);
            this.SetHandler(async (path, days, recursive, verbose) =>
            {
                ParallelConfig config = ParallelConfig.Load();
                if (days <= config.RetentionPeriod) days = config.RetentionPeriod;

                if (string.IsNullOrEmpty(path))
                {
                    await CleanSystemAsync(config, days, recursive, verbose);
                }
                else
                {
                    await CleanDirectoryAsync(config, path, days, recursive, verbose);
                }

            }, _sourceOpt, _daysOpt, _recursiveOpt, _verboseOpt);
        }

        private async Task CleanSystemAsync(ParallelConfig config, int days, bool recursive, bool verbose)
        {
            await System.Threading.Tasks.Parallel.ForEachAsync(config.CleanDirectories, ParallelConfig.Options, async (path, ct) =>
            {
                await CleanDirectoryAsync(config, path, days, recursive, verbose);
            });
        }

        private async Task CleanDirectoryAsync(ParallelConfig config, string path, int days, bool recursive, bool verbose)
        {
            CommandLine.WriteLine($"Scanning for cleanable files older than {days:N0} days old in {path}...", ConsoleColor.DarkGray);
            if (!Directory.Exists(path))
            {
                CommandLine.WriteLine($"The provided path was not found!", ConsoleColor.Yellow);
                return;
            }

            UnixTime minTime = UnixTime.FromMilliseconds(UnixTime.Now.TotalMilliseconds - (days * UnixTime.Day));
            IEnumerable<FileInfo> cleanableFiles = FileScanner.GetCleanableFiles(path, minTime, recursive);
            if (!cleanableFiles.Any()) CommandLine.WriteLine($"No cleanable files were found in the provided path.", ConsoleColor.Green);
            int filesCount = cleanableFiles.Count();

            await System.Threading.Tasks.Parallel.ForEachAsync(cleanableFiles, ParallelConfig.Options, (fi, ct) =>
            {
                if (fi.Exists)
                {
                    try
                    {
                        _freedBytes += fi.Length;
                        fi.Delete();
                    }
                    catch (Exception ex)
                    {
                        CommandLine.WriteLine($"Unable to remove file: {fi.FullName}", ConsoleColor.Yellow);
                        Log.Warning($"{ex.GetBaseException().Message}");
                    }
                }

                return ValueTask.CompletedTask;
            });

            IEnumerable<DirectoryInfo> directories = FileScanner.GetEmptyDirectories(path, recursive);
            if (!directories.Any()) CommandLine.WriteLine($"No empty directories were found in the provided path.", ConsoleColor.Green);
            int directoriesCount = directories.Count();

            await System.Threading.Tasks.Parallel.ForEachAsync(directories, ParallelConfig.Options, (di, ct) =>
            {
                if (di.Exists && di.EnumerateFiles().Any())
                {
                    try
                    {
                        Log.Debug($"Removing empty directory: {di?.FullName}");
                        di?.Delete(true);
                    }
                    catch (Exception ex)
                    {
                        Log.Warning($"{ex.GetBaseException().Message}");
                    }
                }

                return ValueTask.CompletedTask;
            });

            DirectoryInfo currentDir = new DirectoryInfo(path);
            SearchOption option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            if (!currentDir.EnumerateFiles("*", option).Any())
            {
                try
                {
                    Log.Debug($"Removing empty directory: {currentDir.FullName}");
                    currentDir.Delete(true);
                    directoriesCount++;
                }
                catch (Exception ex)
                {
                    Log.Warning($"{ex.GetBaseException().Message}");
                }
            }

            if(filesCount > 0 || directoriesCount > 0) CommandLine.WriteLine($"Successfully cleaned {filesCount:N0} files and {directoriesCount:N0} directories, ({Formatter.FromBytes(_freedBytes)} removed)", ConsoleColor.Green);
        }
    }
}