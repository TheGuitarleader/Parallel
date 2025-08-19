﻿// Copyright 2025 Kyle Ebbinga

using System.Security.Cryptography.X509Certificates;
using Parallel.Core.IO;
using Parallel.Core.Models;

namespace Parallel.Core.Events
{
    public class TransferUpdateEventArgs : EventArgs
    {
        /// <summary>
        /// The recently transferred file.
        /// </summary>
        public SystemFile File { get; }

        /// <summary>
        /// The amount of files already transferred.
        /// </summary>
        public int Finished { get; }

        /// <summary>
        /// The total amount of files to transfer.
        /// </summary>
        public int Total { get; }

        public TransferUpdateEventArgs(SystemFile file, int finished, int total)
        {
            File = file;
            Finished = finished;
            Total = total;
        }
    }
}