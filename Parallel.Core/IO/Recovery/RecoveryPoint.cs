// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Models;

namespace Parallel.Core.IO.Recovery
{
    /// <summary>
    /// Represents a collection of files at an instance of time on the local machine.
    /// </summary>
    public class RecoveryPoint
    {
        /// <summary>
        /// The unique identifier.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The time of creation.
        /// </summary>
        public DateTime CreatedAt { get; }

        /// <summary>
        /// An array of folders to back up.
        /// </summary>
        public string[] BackupFolders { get; }

        /// <summary>
        /// An array of folders to ignore.
        /// </summary>
        public string[] IgnoreFolders { get; }

        /// <summary>
        /// A <see cref="IList{T}"/> collection of files that exist in the local machine.
        /// </summary>
        public List<SystemFile> LocalFiles { get; }

        /// <summary>
        /// A <see cref="IList{T}"/> collection of deleted files that don't exist on the local machine.
        /// </summary>
        public List<SystemFile> DeletedFiles { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="RecoveryPoint"/> class.
        /// </summary>
        public RecoveryPoint(IEnumerable<string> backupFolders, IEnumerable<string> ignoreFolders)
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.Now;
            BackupFolders = backupFolders.ToArray();
            IgnoreFolders = ignoreFolders.ToArray();
            LocalFiles = new List<SystemFile>();
            DeletedFiles = new List<SystemFile>();
        }
    }
}