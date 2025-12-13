// Copyright 2025 Kyle Ebbinga

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using Newtonsoft.Json.Linq;
using Parallel.Core.Database;
using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Security;
using Parallel.Core.Settings;
using Parallel.Core.Workers;

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
            long queued = 0, completed = 0, total = 0;
            TimeSpan uploadTimeout = TimeSpan.FromSeconds(30);
            TimeSpan writeBlockWarn = TimeSpan.FromSeconds(5);

            Channel<UploadWorker> channel = Channel.CreateBounded<UploadWorker>(new BoundedChannelOptions(256)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            });

            Task[] workerTasks = Enumerable.Range(0, ParallelConfig.MaxStaticTransfers).Select(workerId => Task.Run(async () =>
            {
                await foreach (UploadWorker job in channel.Reader.ReadAllAsync())
                {
                    Interlocked.Decrement(ref queued);

                    try
                    {
                        Interlocked.Increment(ref total);
                        string fullPath = PathBuilder.Combine(job.RemotePath, job.Filename);

                        if (await StorageProvider.ExistsAsync(fullPath))
                        {
                            Log.Debug($"[WORKER {workerId}] UPLOAD SKIPPED: {fullPath}");
                            Interlocked.Increment(ref completed);
                            continue;
                        }

                        if (!await StorageProvider.ExistsAsync(job.RemotePath)) await StorageProvider.CreateDirectoryAsync(job.RemotePath);
                        await using MemoryStream ms = new MemoryStream(job.Data, false);

                        using CancellationTokenSource cts = new CancellationTokenSource(uploadTimeout);
                        Task uploadTask = StorageProvider.UploadStreamAsync(ms, fullPath, cts.Token);

                        Task timeoutTask = await Task.WhenAny(uploadTask, Task.Delay(uploadTimeout, cts.Token));
                        if (timeoutTask != uploadTask)
                        {
                            Log.Error($"[WORKER {workerId}] UPLOAD TIMEOUT: {fullPath}");
                            job.OnException?.Invoke(new TimeoutException($"Upload timed out after {uploadTimeout}"));
                            continue;
                        }

                        await uploadTask;
                        Interlocked.Increment(ref completed);
                        Log.Debug($"[WORKER {workerId}] UPLOAD COMPLETE: {fullPath} complete={completed}");
                    }
                    catch (Exception ex)
                    {
                        Log.Error($"[WORKER {workerId}] UPLOAD ERROR: {job.Filename} : {ex}");
                        job.OnException?.Invoke(ex);
                    }
                }
            })).ToArray();

            Task producer = Task.Run(async () =>
            {
                try
                {
                    await System.Threading.Tasks.Parallel.ForEachAsync(files, ParallelConfig.Options, async (file, ct) =>
                    {
                        try
                        {
                            if (file.Deleted)
                            {
                                await Database.AddHistoryAsync(file.LocalPath, HistoryType.Archived);
                                await Database.AddFileAsync(file);
                                progress.Report(ProgressOperation.Archived, file);
                                return;
                            }

                            await using FileStream fs = new FileStream(file.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: ChunkSize, useAsync: true);
                            byte[] buffer = ArrayPool<byte>.Shared.Rent(ChunkSize);

                            try
                            {
                                int index = 0, bytesRead;
                                while ((bytesRead = await fs.ReadAsync(buffer.AsMemory(0, ChunkSize), ct)) > 0)
                                {
                                    byte[] chunk = new byte[bytesRead];
                                    Buffer.BlockCopy(buffer, 0, chunk, 0, bytesRead);

                                    string hash = HashGenerator.CreateSHA256(chunk);
                                    await Database.AddObjectAsync(file.Id, hash, index++);

                                    string basePath = PathBuilder.Combine(RemoteVault.Credentials.RootDirectory, "Parallel", RemoteVault.Id, "objects");
                                    string parentDir = PathBuilder.Combine(basePath, hash.Substring(0, 2), hash.Substring(2, 2));
                                    string remotePath = PathBuilder.Combine(parentDir, hash);

                                    UploadWorker worker = new(chunk, hash, parentDir, ex => progress.Failed(ex, file));
                                    Task writeTask = channel.Writer.WriteAsync(worker, ct).AsTask();
                                    Task timeoutTask = await Task.WhenAny(writeTask, Task.Delay(writeBlockWarn, ct));
                                    if (timeoutTask != writeTask)
                                    {
                                        Log.Warning($"[PRODUCER] channel.Writer.WriteAsync is blocked > {writeBlockWarn} (remote={remotePath})");
                                        await writeTask;
                                    }

                                    Interlocked.Increment(ref queued);
                                }
                            }
                            finally
                            {
                                ArrayPool<byte>.Shared.Return(buffer);
                            }

                            progress.Report(ProgressOperation.Pushed, file);
                            await Database.AddHistoryAsync(file.LocalPath, HistoryType.Pushed);
                            await Database.AddFileAsync(file);
                        }
                        catch (Exception ex)
                        {
                            Log.Error($"[PRODUCER] ERROR: {file.LocalPath}: {ex}");
                            progress.Failed(ex, file);
                        }
                    });
                }
                finally
                {
                    Log.Debug("[PRODUCER] COMPLETE");
                    channel.Writer.Complete();
                }
            });

            Task monitor = Task.Run(async () =>
            {
                Stopwatch sw = Stopwatch.StartNew();
                while (!Task.WhenAll(workerTasks).IsCompleted)
                {
                    Log.Debug($"PIPELINE STATS @ {sw.Elapsed}: queued={queued}, completed={completed}, total={total}");
                    await Task.Delay(1000);
                }
            });

            Log.Debug($"PIPELINE COMPLETED: queued={queued}, completed={completed}, total={total}");
            await Task.WhenAll(producer, monitor).ConfigureAwait(false);
            await Task.WhenAll(workerTasks).ConfigureAwait(false);
        }

        /// <inheritdoc />
        public override async Task PullFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
            long queued = 0, completed = 0, total = 0;
            TimeSpan downloadTimeout = TimeSpan.FromSeconds(30);
            TimeSpan writeBlockWarn = TimeSpan.FromSeconds(5);

            Channel<DownloadWorker> channel = Channel.CreateBounded<DownloadWorker>(new BoundedChannelOptions(256)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            });

            /*Task[] workerTasks = Enumerable.Range(0, ParallelConfig.MaxStaticTransfers).Select(workerId -> Task.Run(async () =>
            {
                try
                {
                    await foreach (DownloadWorker job in channel.Reader.ReadAllAsync())
                    {
                        Interlocked.Decrement(ref queued);

                        try
                        {
                            Interlocked.Increment(ref total);
                        }
                    }
                }
            }));*/
        }
    }
}