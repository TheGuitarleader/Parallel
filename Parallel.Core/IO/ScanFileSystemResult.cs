// Copyright 2025 Kyle Ebbinga

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parallel.Core.Models;

namespace Parallel.Core.IO
{
    public struct ScanFileSystemResult
    {
        public SystemFile[] BackupFiles { get; }
        public SystemFile[] DeletedFiles { get; }
        public SystemFile[] IgnoredFiles { get; }

        public ScanFileSystemResult(IEnumerable<SystemFile> localFiles, IEnumerable<SystemFile> deletedFiles, IEnumerable<SystemFile> ignoredFiles)
        {
            BackupFiles = localFiles.ToArray();
            DeletedFiles = deletedFiles.ToArray();
            IgnoredFiles = ignoredFiles.ToArray();
        }
    }
}