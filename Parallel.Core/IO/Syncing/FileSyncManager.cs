// Copyright 2025 Kyle Ebbinga

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
            if (!files.Any()) return;
            SystemFile[] backupFiles = files.Where(f => !f.Deleted).ToArray();
            Log.Information($"Backing up {backupFiles.Length} files...");
            await StorageProvider.UploadFilesAsync(backupFiles, progress);

            progress.Reset();
            await System.Threading.Tasks.Parallel.ForEachAsync(files, ParallelConfig.Options, async (file, ct) =>
            {
                if (file.Deleted)
                {
                    await Database.AddHistoryAsync(file.LocalPath, HistoryType.Archived);
                    await Database.AddFileAsync(file);
                    progress.Report(ProgressOperation.Archived, file);
                }
                else
                {
                    SystemFile? remote = await StorageProvider.GetFileAsync(file.RemotePath);
                    if (remote is not null)
                    {
                        file.RemoteSize = remote.RemoteSize;
                        await Database.AddHistoryAsync(file.LocalPath, HistoryType.Pushed);
                        await Database.AddFileAsync(file);
                    }

                    progress.Report(ProgressOperation.Synced, file);
                }
            });
        }

        /// <inheritdoc/>
        public override async Task PullFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
            await StorageProvider.DownloadFilesAsync(files, progress);
        }
    }
}