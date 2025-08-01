// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Diagnostics;
using Parallel.Core.Models;

namespace Parallel.Cli.Utils
{
    public class ProgressReport : IProgress
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