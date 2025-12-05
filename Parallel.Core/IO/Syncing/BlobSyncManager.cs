// Copyright 2025 Kyle Ebbinga

using System.Reflection.Metadata;
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
            IEnumerable<string> hashes = await _blobStorage.ChunkFileAsync(files.First().LocalPath, Path.Combine(TempDirectory, "objects"), new ProgressLogger());
            File.WriteAllText(_hashes, JsonConvert.SerializeObject(hashes, Formatting.Indented));
        }

        /// <inheritdoc />
        public override async Task PullFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
            IEnumerable<string>? hashes = JsonConvert.DeserializeObject<IEnumerable<string>>(await File.ReadAllTextAsync(_hashes));
            await _blobStorage.AssembleFileAsync(hashes, Path.Combine(TempDirectory, "objects"), files.First().LocalPath);
        }
    }
}