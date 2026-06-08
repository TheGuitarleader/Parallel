// Copyright 2026 Kyle Ebbinga

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

        public void Report(ProgressOperation operation, LocalFile file)
        {
            Interlocked.Increment(ref _current);

            int percent = _current * 100 / _total;
            //CommandLine.WriteLine($"[{_localVault.Id}] ({percent}%) {operation}: {file.Fullname} {(string.IsNullOrEmpty(file.LocalCheckSum) ? "" : $"({file.LocalCheckSum[..8]})")}");
            CommandLine.Write($"[{_localVault.Id}] ({percent}%) {operation}: {file.Fullname}");
            //CommandLine.ProgressBar(_current, _total, _sw.Elapsed);
        }

        /// <inheritdoc />
        public void Reset()
        {
            _current = 0;
        }

        /// <inheritdoc />
        public void Failed(LocalFile file, string message)
        {
            CommandLine.WriteLine(_localVault, $"Operation failed: '{file.Fullname}' (Reason: {message})", ConsoleColor.Red);
        }
    }
}