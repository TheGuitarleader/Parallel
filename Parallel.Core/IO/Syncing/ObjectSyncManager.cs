// Copyright 2025 Kyle Ebbinga

using System.Collections.Concurrent;
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
        private static readonly ConcurrentDictionary<string, object> _locks = new();

        /// <summary>
        /// The size, in bytes, to use for chunks of a file.
        /// </summary>
        public readonly int ChunkSize = 4194304;

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
                try
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
                        progress.Report(ProgressOperation.Pushing, file);
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
                            if (!await Storage.ExistsAsync(remotePath))
                            {
                                if (!await Storage.ExistsAsync(parentDir)) await Storage.CreateDirectoryAsync(parentDir);
                                await Storage.UploadStreamAsync(ms, remotePath);
                            }
                        }

                        await Database.AddHistoryAsync(file.LocalPath, HistoryType.Pushed);
                    }
                }
                catch (Exception ex)
                {
                    Log.Error(ex.GetBaseException().ToString());
                    progress.Failed(ex, file);
                }
            });
        }

        /// <inheritdoc />
        public override async Task PullFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
            await System.Threading.Tasks.Parallel.ForEachAsync(files, ParallelConfig.Options, async (file, ct) =>
            {
                try
                {
                    progress.Report(ProgressOperation.Pulling, file);
                    string? parentDir = Path.GetDirectoryName(file.LocalPath);
                    if (!Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir);

                    await using FileStream fs = File.Create(file.LocalPath);
                    foreach (string hash in await Database.GetObjectsAsync(file.Id))
                    {
                        string basePath = PathBuilder.Combine(RemoteVault.FileSystem.RootDirectory, "Parallel", RemoteVault.Id, "objects");
                        string remotePath = PathBuilder.Combine(basePath, hash.Substring(0, 2), hash.Substring(2, 2), hash[4..]);
                        if (await Storage.ExistsAsync(remotePath))
                        {
                            await Storage.DownloadStreamAsync(fs, remotePath);
                        }
                    }

                    await Database.AddHistoryAsync(file.LocalPath, HistoryType.Pulled);
                    await fs.FlushAsync(ct);
                }
                catch (Exception ex)
                {
                    Log.Error(ex.GetBaseException().ToString());
                    progress.Failed(ex, file);
                }
            });
        }
    }
}