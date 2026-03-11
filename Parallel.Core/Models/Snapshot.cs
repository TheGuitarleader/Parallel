// Copyright 2026 Kyle Ebbinga

using System.Collections.Concurrent;

namespace Parallel.Core.Models
{
    /// <summary>
    /// Represents a read-only record of a file at a specific moment in time.
    /// </summary>
    public class Snapshot
    {
        /// <summary>
        /// The path of the file on the local machine.
        /// </summary>
        public string LocalPath { get; set; } = string.Empty;
        
        /// <summary>
        /// The time the current file was last written to.
        /// </summary>
        public DateTime LastWrite { get; set; } = DateTime.Now;
        
        /// <summary>
        /// The checksum used to check if the file has changed.
        /// </summary>
        public string? CheckSum { get; set; } = string.Empty;
        
        /// <summary>
        /// If the file is currently hidden.
        /// </summary>
        public bool Hidden { get; } = false;

        /// <summary>
        /// If the file is currently read-only.
        /// </summary>
        public bool ReadOnly { get; } = false;

        public Snapshot(SystemFile file)
        {
            LocalPath = file.LocalPath;
            LastWrite = file.LastWrite.ToLocalTime();
            CheckSum = file.CheckSum;
            Hidden = file.Hidden;
            ReadOnly = file.ReadOnly;
        }
    }
}