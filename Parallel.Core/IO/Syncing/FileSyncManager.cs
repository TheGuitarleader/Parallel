// Copyright 2026 Kyle Ebbinga

using System.Collections.Concurrent;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Parallel.Core.Database;
using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Security;
using Parallel.Core.Settings;
using Parallel.Core.Utils;

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
        public override async Task<int> BackupFilesAsync(IReadOnlyList<LocalFile> files, IProgressReporter progress, bool overwrite)
        {
            if (!files.Any()) return 0;
            int completed = 0;

            ConcurrentBag<LocalFile> cleanupFiles = [];
            ConcurrentDictionary<string, SemaphoreSlim> threadPool = new ConcurrentDictionary<string, SemaphoreSlim>();
            LocalFile[] uploadFiles = files.Where(f => !f.Deleted).ToArray();
            LocalFile[] deletedFiles = files.Except(uploadFiles).ToArray();

            Log.Information("Uploading {UploadFilesLength:N0} files...", uploadFiles.Length);
            await System.Threading.Tasks.Parallel.ForEachAsync(uploadFiles, ParallelConfig.Options, async (file, ct) =>
            {
                if (!file.TryGenerateCheckSum()) return;

                SemaphoreSlim threadLock = threadPool.GetOrAdd(file.RemoteCheckSum!, _ => new SemaphoreSlim(1, 1));
                string remotePath = PathBuilder.GetObjectPath(RemoteVault, file.RemoteCheckSum!);
                await threadLock.WaitAsync(ct);

                try
                {
                    RemoteFile? uploadedFile = await StorageProvider.UploadFileAsync(file, remotePath, overwrite, ct);
                    if (uploadedFile != null && file.RemoteCheckSum == uploadedFile.RemoteCheckSum)
                    {
                        LocalFile localFile = file.AppendFile(uploadedFile);
                        if (!await (Database?.AddHistoryAsync(HistoryType.Synced, localFile) ?? Task.FromResult(false))) Log.Error("Failed to add history: {Fullname}", localFile.Fullname);
                        if (!await (Database?.AddFileAsync(localFile) ?? Task.FromResult(false))) Log.Error("Failed to add file: {Fullname}", localFile.Fullname);
                        progress.Report(ProgressOperation.Synced, localFile);
                        Interlocked.Increment(ref completed);
                    }
                    else
                    {
                        Log.Warning("File failed during upload: {Fullname}", file.Fullname);
                    }
                }
                catch (Exception ex)
                {
                    progress.Failed(file, ex.GetBaseException().ToString());
                }
                finally
                {
                    threadLock.Release();
                }
            });

            Log.Information("Archiving {DeletedFilesLength:N0} files...", deletedFiles.Length);
            await System.Threading.Tasks.Parallel.ForEachAsync(deletedFiles, ParallelConfig.Options, async (file, ct) =>
            {
                if (!await (Database?.AddHistoryAsync(HistoryType.Archived, file) ?? Task.FromResult(false))) Log.Error("Failed to add history: {Fullname}", file.Fullname);
                if (!await (Database?.AddFileAsync(file) ?? Task.FromResult(false))) Log.Error("Failed to add file: {Fullname}", file.Fullname);
                progress.Report(ProgressOperation.Archived, file);
                Interlocked.Increment(ref completed);
            });
            
            return completed;
        }

        /// <inheritdoc/>
        public override async Task<int> RestoreFilesAsync(IReadOnlyList<LocalFile> files, IProgressReporter progress)
        {
            if (!files.Any()) return 0;
            int queued = 0, completed = 0, total = files.Count;

            ConcurrentDictionary<string, SemaphoreSlim> threadPool = new ConcurrentDictionary<string, SemaphoreSlim>();
            Task worker = System.Threading.Tasks.Parallel.ForEachAsync(files, ParallelConfig.Options, async (file, ct) =>
            {
                Interlocked.Increment(ref queued);
                SemaphoreSlim threadLock = threadPool.GetOrAdd(file.LocalCheckSum!, _ => new SemaphoreSlim(1, 1));
                await threadLock.WaitAsync(ct);

                try
                {
                    string? parentDir = Path.GetDirectoryName(file.Fullname);
                    if (!string.IsNullOrEmpty(parentDir) && !Directory.Exists(parentDir)) Directory.CreateDirectory(parentDir);

                    Log.Debug($"Restoring file: {file.Fullname} ({file.RemoteCheckSum})");
                    await StorageProvider.DownloadFileAsync(file.Fullname, PathBuilder.GetObjectPath(RemoteVault, file.RemoteCheckSum!), ct);
                    if (!File.Exists(file.Fullname))
                    {
                        progress.Failed(file, "File not found!");
                        return;
                    }

                    FileInfo fileInfo = new(file.Fullname);
                    FileAttributes attributes = fileInfo.Attributes;
                    if (file.ReadOnly) attributes |= FileAttributes.ReadOnly;
                    if (file.Hidden) attributes |= FileAttributes.Hidden;
                    fileInfo.LastWriteTime = file.LastWrite.ToLocalTime();
                    fileInfo.Attributes = attributes;

                    if (!await (Database?.AddHistoryAsync(HistoryType.Restored, file) ?? Task.FromResult(false))) Log.Error("Failed to add history: {Fullname}", file.Fullname);
                    progress.Report(ProgressOperation.Restored, file);
                }
                catch (Exception ex)
                {
                    progress.Failed(file, ex.GetBaseException().ToString());
                    if (File.Exists(file.Fullname)) File.Delete(file.Fullname);
                }
                finally
                {
                    threadLock.Release();
                    threadPool.TryRemove(file.LocalCheckSum!, out _);
                    Interlocked.Increment(ref completed);
                    Interlocked.Decrement(ref queued);
                }
            });

            Task monitor = Task.Run(async () =>
            {
                Stopwatch sw = Stopwatch.StartNew();
                while (!worker.IsCompleted)
                {
                    Log.Debug($"WORKER STATS @ ETA {Converter.ToRemainingTimeSpan(sw.Elapsed, completed, total)}: queued={queued}, completed={completed}, total={total}");
                    await Task.Delay(10000);
                }
            });

            await Task.WhenAll(worker, monitor).ConfigureAwait(false);
            return completed;
        }

        /// <inheritdoc/>
        public override async Task<int> PruneFilesAsync(IReadOnlyList<LocalFile> files, IProgressReporter progress)
        {
            if (!files.Any()) return 0;
            int queued = 0, completed = 0, total = files.Count;

            Task worker = System.Threading.Tasks.Parallel.ForEachAsync(files, ParallelConfig.Options, async (file, ct) =>
            {
                Interlocked.Increment(ref queued);

                try
                {
                    await (Database != null ? Database.RemoveFileAsync(file) : Task.CompletedTask);
                    if (!await (Database?.AddHistoryAsync(HistoryType.Pruned, file) ?? Task.FromResult(false))) Log.Error("Failed to add history: {Fullname}", file.Fullname);
                    await StorageProvider.DeleteFileAsync(PathBuilder.GetObjectPath(RemoteVault, file.LocalCheckSum!));
                    progress.Report(ProgressOperation.Pruned, file);
                }
                catch (Exception ex)
                {
                    progress.Failed(file, ex.GetBaseException().ToString());
                }
                finally
                {
                    Interlocked.Increment(ref completed);
                    Interlocked.Decrement(ref queued);
                }
            });

            Task monitor = Task.Run(async () =>
            {
                Stopwatch sw = Stopwatch.StartNew();
                while (!worker.IsCompleted)
                {
                    Log.Debug($"WORKER STATS @ ETA {Converter.ToRemainingTimeSpan(sw.Elapsed, completed, total)}: queued={queued}, completed={completed}, total={total}");
                    await Task.Delay(10000);
                }
            });

            await Task.WhenAll(worker, monitor).ConfigureAwait(false);
            return completed;
        }
    }
}