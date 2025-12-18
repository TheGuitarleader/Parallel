// Copyright 2025 Kyle Ebbinga

using System.Diagnostics;
using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Settings;

namespace Parallel.Cli.Utils
{
    public class ProgressReport : IProgressReporter
    {
        private Stopwatch _sw;
        private readonly LocalVaultConfig _localVault;
        private int _current;
        private readonly int _total;

        public ProgressReport(LocalVaultConfig localVault, int totalFiles)
        {
            _sw = Stopwatch.StartNew();
            _localVault = localVault;
            _current = 0;
            _total = totalFiles;
        }

        public void Report(ProgressOperation operation, SystemFile file)
        {
            int percent = _current++ * 100 / _total;
            CommandLine.WriteLine($"[{_localVault.Id}] ({percent}%) {operation}: {file.LocalPath}");
            //CommandLine.ProgressBar(_current++, _total, _sw.Elapsed);
        }

        /// <inheritdoc />
        public void Reset()
        {
            _current = 0;
        }

        public void Failed(Exception exception, SystemFile file)
        {
            CommandLine.WriteLine(_localVault, $"Failed to upload file: '{file.LocalPath}'", ConsoleColor.Red);
        }
    }
}