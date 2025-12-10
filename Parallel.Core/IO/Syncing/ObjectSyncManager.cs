// Copyright 2025 Kyle Ebbinga

using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Threading.Channels;
using Newtonsoft.Json.Linq;
using Parallel.Core.Database;
using Parallel.Core.Diagnostics;
using Parallel.Core.IO.Blobs;
using Parallel.Core.Models;
using Parallel.Core.Security;
using Parallel.Core.Settings;
using Parallel.Core.Utils;

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
            TimeSpan uploadTimeout = TimeSpan.FromSeconds(30);
            TimeSpan writeBlockWarn = TimeSpan.FromSeconds(5);
            long enqueued = 0, dequeued = 0, uploadStarted = 0, uploadDone = 0;
            Channel<TransferWorker> channel = Channel.CreateBounded<TransferWorker>(new BoundedChannelOptions(256)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = false,
                SingleWriter = false
            });

            // WORKERS
            Task[] workerTasks = Enumerable.Range(0, ParallelConfig.MaxTransfers).Select(workerId => Task.Run(async () =>
            {
                Log.Debug($"[WORKER {workerId}] START");
                try
                {
                    await foreach (TransferWorker job in channel.Reader.ReadAllAsync())
                    {
                        Interlocked.Increment(ref dequeued);
                        Log.Debug($"[WORKER {workerId}] DEQUEUE -> {job.RemotePath} dequeued={dequeued}");

                        try
                        {
                            Interlocked.Increment(ref uploadStarted);
                            Log.Debug($"[WORKER {workerId}] UPLOAD START -> {job.RemotePath} started={uploadStarted}");

                            string fullPath = PathBuilder.Combine(job.RemotePath, job.Filename);
                            if (await Storage.ExistsAsync(fullPath))
                            {
                                Log.Debug($"[WORKER {workerId}] UPLOAD SKIPPED -> {job.RemotePath} done={uploadDone}");
                                continue;
                            }

                            if (!await Storage.ExistsAsync(job.RemotePath))
                                await Storage.CreateDirectoryAsync(job.RemotePath);

                            await using MemoryStream ms = new MemoryStream(job.Data, writable: false);

                            using CancellationTokenSource cts = new CancellationTokenSource(uploadTimeout);
                            Task uploadTask = Storage.UploadStreamAsync(ms, fullPath);
                            await uploadTask;

                            Interlocked.Increment(ref uploadDone);
                            Log.Debug($"[WORKER {workerId}] UPLOAD DONE -> {job.RemotePath} done={uploadDone}");
                        }
                        catch (Exception wex)
                        {
                            Log.Error($"[WORKER {workerId}] UPLOAD EX -> {job.RemotePath} : {wex}");
                            job.OnError?.Invoke(wex);
                        }
                    }

                    Log.Debug($"[WORKER {workerId}] READER COMPLETED");
                }
                finally
                {
                    Log.Debug($"[WORKER {workerId}] EXIT");
                }
            })).ToArray();

            // PRODUCER
            var producer = Task.Run(async () =>
            {
                Log.Debug("PRODUCER START");
                try
                {
                    await System.Threading.Tasks.Parallel.ForEachAsync(files, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }, async (file, token) =>
                    {
                        try
                        {
                            if (file.Deleted)
                            {
                                progress.Report(ProgressOperation.Archiving, file);
                                await Database.AddHistoryAsync(file.LocalPath, HistoryType.Archived);
                                await Database.AddFileAsync(file);
                                return;
                            }

                            progress.Report(ProgressOperation.Pushing, file);
                            await Database.AddFileAsync(file);

                            await using FileStream fs = new FileStream(file.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: ChunkSize, useAsync: true);

                            byte[] buffer = ArrayPool<byte>.Shared.Rent(ChunkSize);
                            try
                            {
                                int index = 0;
                                int bytesRead;
                                while ((bytesRead = await fs.ReadAsync(buffer.AsMemory(0, ChunkSize), token)) > 0)
                                {
                                    byte[] chunk = new byte[bytesRead];
                                    Buffer.BlockCopy(buffer, 0, chunk, 0, bytesRead);

                                    string hash = HashGenerator.CreateSHA256(chunk);
                                    await Database.AddObjectAsync(file.Id, hash, index++);

                                    string basePath = PathBuilder.Combine(RemoteVault.FileSystem.RootDirectory, "Parallel", RemoteVault.Id, "objects");
                                    string parentDir = PathBuilder.Combine(basePath, hash.Substring(0, 2), hash.Substring(2, 2));
                                    string remotePath = PathBuilder.Combine(parentDir, hash[4..]);

                                    TransferWorker job = new TransferWorker(chunk, parentDir, hash[4..], ex => progress.Failed(ex, file));

                                    Task writeTask = channel.Writer.WriteAsync(job, token).AsTask();
                                    Task winner = await Task.WhenAny(writeTask, Task.Delay(writeBlockWarn, token));
                                    if (winner != writeTask)
                                    {
                                        Log.Warning($"PRODUCER: channel.Writer.WriteAsync is blocked > {writeBlockWarn} (remote={remotePath}, enq={enqueued}, deq={dequeued})");
                                        await writeTask;
                                    }

                                    Interlocked.Increment(ref enqueued);
                                    Log.Debug($"ENQUEUE -> {remotePath}. enqueued={enqueued}");
                                }
                            }
                            finally
                            {
                                ArrayPool<byte>.Shared.Return(buffer);
                            }

                            await Database.AddHistoryAsync(file.LocalPath, HistoryType.Pushed);
                        }
                        catch (Exception pex)
                        {
                            Log.Error($"PRODUCER EX processing {file.LocalPath}: {pex}");
                            progress.Failed(pex, file);
                        }
                    });
                }
                finally
                {
                    Log.Debug("PRODUCER COMPLETE -> Completing writer");
                    channel.Writer.Complete();
                }
            });

            // MONITOR
            Task monitor = Task.Run(async () =>
            {
                Stopwatch sw = Stopwatch.StartNew();
                while (!Task.WhenAll(workerTasks).IsCompleted)
                {
                    Log.Debug($"PIPELINE STATS @ {sw.Elapsed}: enq={enqueued}, deq={dequeued}, upStarted={uploadStarted}, upDone={uploadDone}");
                    await Task.Delay(1000);
                }
            });

            await Task.WhenAll(producer, monitor).ConfigureAwait(false);
            await Task.WhenAll(workerTasks).ConfigureAwait(false);
            Log.Debug("PUSHFILES FINISHED");
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