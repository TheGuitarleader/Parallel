// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Net;

namespace Parallel.Service.Utils
{
    public class UdpReporting : IProgressReporter
    {
        private readonly Communication _comms = new Communication();

        public void Report(ProgressOperation operation, SystemFile file, int current, int total)
        {
            int percent = current * 100 / total;
            //_comms.Send($"[{percent}%] {operation}: {file.LocalPath}");
        }

        public void Failed(Exception exception, SystemFile file)
        {
            //_comms.Send($"Failed to upload file: '{file.LocalPath}'");
        }
    }
}