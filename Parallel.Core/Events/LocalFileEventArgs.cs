// Copyright 2025 Kyle Ebbinga

using Parallel.Core.IO;
using System;
using System.IO;
using Parallel.Core.Models;

namespace Parallel.Core.Events
{
    public class LocalFileEventArgs : EventArgs
    {
        public SystemFile SystemFile { get; }

        public LocalFileEventArgs(string file)
        {
            SystemFile = new SystemFile(new FileInfo(file));
        }
    }
}