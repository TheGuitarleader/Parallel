// Copyright 2025 Kyle Ebbinga

using System.Collections.Concurrent;
using System.Diagnostics;
using Parallel.Core.Database;
using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Settings;

namespace Parallel.Core.IO.Syncing
{
    /// <summary>
    /// Represents the way to sync whole files to an associated file system.
    /// </summary>
    public class FileSyncManager : BaseSyncManager
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileSyncManager"/> class.
        /// </summary>
        /// <param name="localVault"></param>
        public FileSyncManager(LocalVaultConfig localVault) : base(localVault) { }

        /// <inheritdoc/>
        public override async Task PushFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
            if (files.Length == 0) return;
            long queued = 0, completed = 0;

            ConcurrentDictionary<string, SemaphoreSlim> threadPool = new ConcurrentDictionary<string, SemaphoreSlim>();
            SystemFile[] uploadFiles = files.Where(f => f is { Deleted: false, LocalSize: > 0 }).ToArray();
            SystemFile[] deleteFiles = files.Except(uploadFiles).ToArray();

            Log.Information($"Pushing {uploadFiles.Length:N0} files...");
            Task worker = System.Threading.Tasks.Parallel.ForEachAsync(uploadFiles, ParallelConfig.Options, async (file, ct) =>
            {
                Interlocked.Increment(ref queued);
                SemaphoreSlim lockedThread = threadPool.GetOrAdd(file.CheckSum, _ => new SemaphoreSlim(1, 1));
                await lockedThread.WaitAsync(ct);

                try
                {
                    Log.Debug($"Pushing -> {file.LocalPath}");
                    file.RemotePath = PathBuilder.GetObjectPath(RemoteVault, file.CheckSum);
                    long result = await StorageProvider.UploadFileAsync(file, false, ct);
                    if (result <= 0)
                    {
                        progress.Failed(new InvalidOperationException(), file);
                        return;
                    }

                    file.RemoteSize = result;
                    await Database.AddFileAsync(file);
                    await Database.AddHistoryAsync(HistoryType.Pushed, file);
                    progress.Report(ProgressOperation.Pushed, file);
                }
                finally
                {
                    lockedThread.Release();
                    if (lockedThread.CurrentCount == 1)
                    {
                        threadPool.TryRemove(file.CheckSum, out _);
                        Interlocked.Increment(ref completed);
                        Interlocked.Decrement(ref queued);
                    }
                }
            });

            Log.Information($"Archiving {deleteFiles.Length:N0} files...");
            foreach (SystemFile file in deleteFiles)
            {
                await Database.AddFileAsync(file);
                await Database.AddHistoryAsync(HistoryType.Archived, file);
                progress.Report(ProgressOperation.Archived, file);
            }

            Task monitor = Task.Run(async () =>
            {
                Stopwatch sw = Stopwatch.StartNew();
                while (!worker.IsCompleted)
                {
                    Log.Debug($"WORKER STATS @ {sw.Elapsed}: queued={queued}, completed={completed}");
                    await Task.Delay(1000);
                }
            });

            await Task.WhenAll(worker, monitor).ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public override async Task PullFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
            if (files.Length == 0) return;
            long queued = 0, completed = 0;

            ConcurrentDictionary<string, SemaphoreSlim> threadPool = new ConcurrentDictionary<string, SemaphoreSlim>();
            Task worker = System.Threading.Tasks.Parallel.ForEachAsync(files, ParallelConfig.Options, async (file, ct) =>
            {
                Interlocked.Increment(ref queued);
                SemaphoreSlim lockedThread = threadPool.GetOrAdd(file.CheckSum, _ => new SemaphoreSlim(1, 1));
                await lockedThread.WaitAsync(ct);

                try
                {
                    await StorageProvider.DownloadFileAsync(file, ct);
                    if (!File.Exists(file.LocalPath))
                    {
                        progress.Failed(new InvalidOperationException(), file);
                        return;
                    }

                    FileInfo fileInfo = new(file.LocalPath);
                    FileAttributes attributes = fileInfo.Attributes;
                    if (file.ReadOnly) attributes |= FileAttributes.ReadOnly;
                    if (file.Hidden) attributes |= FileAttributes.Hidden;
                    fileInfo.LastWriteTime = file.LastWrite.ToLocalTime();
                    fileInfo.Attributes = attributes;

                    await Database.AddHistoryAsync(HistoryType.Pulled, file);
                    progress.Report(ProgressOperation.Pulled, file);
                }
                finally
                {
                    lockedThread.Release();
                    if (lockedThread.CurrentCount == 1)
                    {
                        threadPool.TryRemove(file.CheckSum, out _);
                        Interlocked.Increment(ref completed);
                        Interlocked.Decrement(ref queued);
                    }
                }
            });

            Task monitor = Task.Run(async () =>
            {
                Stopwatch sw = Stopwatch.StartNew();
                while (!worker.IsCompleted)
                {
                    Log.Debug($"WORKER STATS @ {sw.Elapsed}: queued={queued}, completed={completed}");
                    await Task.Delay(1000);
                }
            });

            await Task.WhenAll(worker, monitor).ConfigureAwait(false);
        }
    }
}