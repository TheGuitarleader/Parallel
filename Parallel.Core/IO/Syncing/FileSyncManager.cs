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
            if (files.Length == 0) return;
            SystemFile[] uploadFiles = files.Where(f => !f.Deleted).ToArray();
            SystemFile[] deleteFiles = files.Where(f => f.Deleted).ToArray();

            Log.Information($"Pushing {uploadFiles.Length:N0} files...");
            await System.Threading.Tasks.Parallel.ForEachAsync(uploadFiles, ParallelConfig.Options, async (file, ct) =>
            {
                file.RemotePath = PathBuilder.GetObjectPath(RemoteVault, file.Id);
                SystemFile? result = await StorageProvider.UploadFileAsync(file, ct);
                if (result is null) return;

                await Database.AddFileAsync(result);
                await Database.AddHistoryAsync(result.LocalPath, HistoryType.Pushed);
                progress.Report(ProgressOperation.Pushed, file);
            });

            Log.Information($"Archiving {deleteFiles.Length:N0} files...");
            foreach (SystemFile file in deleteFiles)
            {
                await Database.AddFileAsync(file);
                await Database.AddHistoryAsync(file.LocalPath, HistoryType.Archived);
                progress.Report(ProgressOperation.Archived, file);
            }
        }

        /// <inheritdoc/>
        public override async Task PullFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
            if (files.Length == 0) return;
            SystemFile[] downloadFiles = files.Where(f => !f.Deleted).ToArray();
            await System.Threading.Tasks.Parallel.ForEachAsync(downloadFiles, ParallelConfig.Options, async (file, ct) =>
            {

            });
        }
    }
}