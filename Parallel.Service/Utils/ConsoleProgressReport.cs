// Copyright 2026 Kyle Ebbinga

using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Serilog;

namespace Parallel.Service.Utils
{
    public class ConsoleProgressReport : IProgressReporter
    {
        public void Report(ProgressOperation operation, SystemFile file)
        {
            Log.Debug($"{operation}: {file.LocalPath}");
        }

        public void Reset() { }

        public void Failed(SystemFile file, string reason)
        {
            Log.Error($"Operation failed: '{file.LocalPath}' (Reason: {reason})");
        }
    }
}