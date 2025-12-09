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
    public class ObjectSyncManager : BaseSyncManager
    {
        /// <summary>
        /// The size, in bytes, to use for chunks of a file.
        /// </summary>
        private static readonly int ChunkSize = 4194304;


        /// <summary>
        /// Initializes a new instance of the <see cref="ObjectSyncManager"/> class.
        /// </summary>
        /// <param name="localVault"></param>
        public ObjectSyncManager(LocalVaultConfig localVault) : base(localVault) { }

        /// <inheritdoc />
        public override async Task PushFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
            await System.Threading.Tasks.Parallel.ForEachAsync(files, ParallelConfig.Options, async (file, ct) =>
            {
                if (file.Deleted)
                {
                    progress.Report(ProgressOperation.Archiving, file);
                    await Database.AddHistoryAsync(file.LocalPath, HistoryType.Archived);
                    await Database.AddFileAsync(file);
                }
                else
                {
                    int bytesRead = 0;
                    byte[] buffer = new byte[ChunkSize];
                    await using FileStream fs = File.OpenRead(file.LocalPath);

                    int index = 0;
                    progress.Report(ProgressOperation.Uploading, file);
                    await Database.AddFileAsync(file);

                    while ((bytesRead = await fs.ReadAsync(buffer, ct)) > 0)
                    {
                        await using MemoryStream ms = new MemoryStream(buffer, 0, bytesRead);
                        string hash = HashGenerator.CreateSHA256(buffer.AsSpan(0, bytesRead));
                        await Database.AddObjectAsync(file.Id, hash, index);
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
                }
            });
        }

        /// <inheritdoc />
        public override async Task PullFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
            await System.Threading.Tasks.Parallel.ForEachAsync(files, ParallelConfig.Options, async (file, ct) =>
            {
                progress.Report(ProgressOperation.Downloading, file);
                string? parentDir = Path.GetDirectoryName(file.LocalPath);
                if (!Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir);

                await using FileStream fs = File.Create(file.LocalPath);
                foreach (string hash in await Database.GetObjectsAsync(file.Id))
                {
                    string basePath = PathBuilder.Combine(RemoteVault.FileSystem.RootDirectory, "Parallel", RemoteVault.Id, "objects");
                    string remotePath = PathBuilder.Combine(basePath, hash.Substring(0, 2), hash.Substring(2, 2), hash[4..]);
                    if (await FileSystem.ExistsAsync(remotePath))
                    {
                        await FileSystem.DownloadStreamAsync(fs, remotePath);
                    }
                }

                await fs.FlushAsync(ct);
            });
        }
    }
}