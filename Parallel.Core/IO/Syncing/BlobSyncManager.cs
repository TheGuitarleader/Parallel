// Copyright 2025 Kyle Ebbinga

using System.Reflection.Metadata;
using Parallel.Core.Database;
using Parallel.Core.Diagnostics;
using Parallel.Core.IO.Blobs;
using Parallel.Core.Models;
using Parallel.Core.Settings;

namespace Parallel.Core.IO.Syncing
{
    /// <summary>
    /// Represents the way to sync files with content assigned binary objects.
    /// </summary>
    public class BlobSyncManager : BaseSyncManager
    {
        private readonly BlobStorage _blobStorage;
        private string _hashes;

        /// <summary>
        /// Initializes a new instance of the <see cref="BlobSyncManager"/> class.
        /// </summary>
        /// <param name="localVault"></param>
        public BlobSyncManager(LocalVaultConfig localVault) : base(localVault)
        {
            _blobStorage = new BlobStorage(TempDirectory);
            _hashes = Path.Combine(TempDirectory, "Hashes.json");
        }

        /// <inheritdoc />
        public override async Task PushFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
            await System.Threading.Tasks.Parallel.ForEachAsync(files, ParallelConfig.Options, async (file, ct) =>
            {
                await _blobStorage.ChunkFileAsync(file.LocalPath, Path.Combine(TempDirectory, "objects"), new ProgressLogger());
            });
        }

        /// <inheritdoc />
        public override async Task PullFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
        }
    }
}