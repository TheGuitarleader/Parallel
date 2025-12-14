// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Settings;

namespace Parallel.Core.IO.Syncing
{
    /// <summary>
    /// Represents the way to sync files to an associated file system using file deltas.
    /// </summary>
    public class DeltaSyncManager : BaseSyncManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DeltaSyncManager"/> class.
        /// </summary>
        /// <param name="remoteVaultConfig"></param>
        public DeltaSyncManager(RemoteVaultConfig remoteVaultConfig) : base(remoteVaultConfig) { }

        /// <inheritdoc />
        public override Task<int> PushFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override Task<int> PullFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
            throw new NotImplementedException();
        }
    }
}