// Copyright 2026 Kyle Ebbinga

using Parallel.Core.IO;
using System;
using System.IO;
using Parallel.Core.Models;

namespace Parallel.Core.Events
{
    public class LocalFileEventArgs : EventArgs
    {
        public LocalFile LocalFile { get; }

        public LocalFileEventArgs(string file)
        {
            LocalFile = new LocalFile(file);
        }
    }
}