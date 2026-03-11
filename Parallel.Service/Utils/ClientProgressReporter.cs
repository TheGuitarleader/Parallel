// Copyright 2026 Kyle Ebbinga

using Parallel.Core.Diagnostics;
using Parallel.Core.Models;

namespace Parallel.Service.Utils
{
    public class ClientProgressReporter : IProgressReporter
    {
        public void Report(ProgressOperation operation, SystemFile file)
        {
            throw new NotImplementedException();
        }

        public void Reset()
        {
            throw new NotImplementedException();
        }

        public void Failed(SystemFile file, string reason)
        {
            throw new NotImplementedException();
        }
    }
}