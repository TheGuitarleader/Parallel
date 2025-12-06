// Copyright 2025 Kyle Ebbinga

using System.Reflection.Metadata;
using Newtonsoft.Json.Linq;
using Parallel.Core.Database;
using Parallel.Core.Diagnostics;
using Parallel.Core.IO.Blobs;
using Parallel.Core.Models;
using Parallel.Core.Security;
using Parallel.Core.Settings;

namespace Parallel.Core.IO.Syncing
{
    /// <summary>
    /// Represents the way to sync files with content assigned binary objects.
    /// </summary>
    public class BlobSyncManager : BaseSyncManager
    {
        /// <summary>
        /// The size, in bytes, to use for chunks of a file.
        /// </summary>
        private static readonly int ChunkSize = 4194304;


        /// <summary>
        /// Initializes a new instance of the <see cref="BlobSyncManager"/> class.
        /// </summary>
        /// <param name="localVault"></param>
        public BlobSyncManager(LocalVaultConfig localVault) : base(localVault) { }

        /// <inheritdoc />
        public override async Task PushFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
            await System.Threading.Tasks.Parallel.ForEachAsync(files, ParallelConfig.Options, async (file, ct) =>
            {
                int bytesRead = 0;
                byte[] buffer = new byte[ChunkSize];
                await using FileStream fs = File.OpenRead(file.LocalPath);

                int index = 0;
                progress.Report(ProgressOperation.Uploading, file);
                int row = await Database.AddManifestAsync(file);
                while ((bytesRead = await fs.ReadAsync(buffer, ct)) > 0)
                {
                    await using MemoryStream ms = new MemoryStream(buffer, 0, bytesRead);
                    string hash = HashGenerator.CreateSHA256(buffer.AsSpan(0, bytesRead));
                    await Database.AddChunkAsync(row, hash, index);
                    index++;

                    string basePath = PathBuilder.Combine(RemoteVault.FileSystem.RootDirectory, "Parallel", RemoteVault.Id, "objects");
                    string parentDir = PathBuilder.Combine(basePath, hash.Substring(0, 2), hash.Substring(2, 2));
                    string remotePath = PathBuilder.Combine(parentDir, hash[4..]);
                    if (!await FileSystem.ExistsAsync(remotePath))
                    {
                        if (!await FileSystem.ExistsAsync(parentDir)) await FileSystem.CreateDirectoryAsync(parentDir);
                        await FileSystem.UploadStreamAsync(ms, remotePath);
                    }
                }
            });
        }

        /// <inheritdoc />
        public override async Task PullFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
        }
    }
}