﻿// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Settings;

namespace Parallel.Cli.Utils
{
    public class ProgressReport : IProgressReporter
    {
        private readonly LocalVaultConfig _localVault;
        private int _current;
        private int _total;

        public ProgressReport(LocalVaultConfig localVault, int totalFiles)
        {
            _localVault = localVault;
            _current = 0;
            _total = totalFiles;
        }

        public void Report(ProgressOperation operation, SystemFile file)
        {
            int percent = _current++ * 100 / _total;
            CommandLine.WriteLine($"[{percent}%] <{_localVault.Id}> {operation}: {file.LocalPath}");
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