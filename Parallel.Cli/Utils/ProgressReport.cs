// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Settings;

namespace Parallel.Cli.Utils
{
    public class ProgressReport : IProgressReporter
    {
        public void Report(ProgressOperation operation, SystemFile file, int current, int total)
        {
            int percent = current * 100 / total;
            CommandLine.WriteLine($"[{percent}%] {operation}: {file.LocalPath}");
        }

        public void Failed(Exception exception, SystemFile file)
        {
            CommandLine.WriteLine($"Failed to upload file: '{file.LocalPath}'", ConsoleColor.Red);
        }
    }
}