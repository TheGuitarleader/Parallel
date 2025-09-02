// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Settings;

namespace Parallel.Cli.Utils
{
    public class ProgressReport : IProgressReporter
    {
        private readonly LocalVaultConfig _localVault;
        private readonly int _totalFiles;

        public ProgressReport(LocalVaultConfig localVault, int totalFiles)
        {
            _localVault = localVault;
            _totalFiles = totalFiles;
        }

        public void Report(ProgressOperation operation, SystemFile file, int current, int total)
        {
            int percent = current * 100 / total;
            CommandLine.WriteLine($"[{percent}%] <{_localVault.Id}> {operation}: {file.LocalPath}");
        }

        public void Failed(Exception exception, SystemFile file)
        {
            CommandLine.WriteLine(_localVault, $"Failed to upload file: '{file.LocalPath}'", ConsoleColor.Red);
        }
    }
}