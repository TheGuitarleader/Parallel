// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Settings;

namespace Parallel.Core.IO.Syncing
{
    /// <summary>
    /// Represents the way to clone files to an associated file system using file deltas.
    /// </summary>
    public class DeltaSyncManager : BaseSyncManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeltaSyncManager"/> class.
        /// </summary>
        /// <param name="vault"></param>
        public DeltaSyncManager(VaultConfig vault) : base(vault) { }

        /// <inheritdoc />
        public override Task PushFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override Task PullFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
            throw new NotImplementedException();
        }
    }
}