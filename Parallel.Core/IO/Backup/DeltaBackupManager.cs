// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Settings;

namespace Parallel.Core.IO.Backup
{
    /// <summary>
    /// Represents the way to clone files to an associated file system using file deltas.
    /// </summary>
    public class DeltaBackupManager : BaseFileManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeltaBackupManager"/> class.
        /// </summary>
        /// <param name="profile"></param>
        public DeltaBackupManager(ProfileConfig profile) : base(profile) { }

        /// <inheritdoc />
        public override Task BackupFilesAsync(SystemFile[] files, IProgress progress)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override Task RestoreFilesAsync(SystemFile[] files, IProgress progress)
        {
            throw new NotImplementedException();
        }
    }
}