// Copyright 2026 Kyle Ebbinga

using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Serilog;

namespace Parallel.Service.Utils
{
    public class ConsoleProgressReport : IProgressReporter
    {
        public void Report(ProgressOperation operation, LocalFile file)
        {
            Log.Debug($"{operation}: {file.Fullname}");
        }

        public void Reset() { }

        public void Failed(LocalFile file, string reason)
        {
            Log.Error($"Operation failed: '{file.Fullname}' (Reason: {reason})");
        }
    }
}