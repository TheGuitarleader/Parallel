// Copyright 2025 Kyle Ebbinga

using System.Security.Cryptography.X509Certificates;
using Parallel.Core.IO;
using Parallel.Core.Models;

namespace Parallel.Core.Events
{
    public class TransferFailedEventArgs : EventArgs
    {
        public SystemFile File { get; }
        public Exception Exception { get; }

        public TransferFailedEventArgs(SystemFile file, Exception exception)
        {
            File = file;
            Exception = exception;
        }
    }
}