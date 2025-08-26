// Copyright 2025 Kyle Ebbinga

using Newtonsoft.Json.Linq;
using Parallel.Core.Database;
using Parallel.Core.Diagnostics;
using Parallel.Core.Events;
using Parallel.Core.IO.FileSystem;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Models;
using Parallel.Core.Settings;

namespace Parallel.Core.IO.Backup
{
    /// <summary>
    /// Represents the way to archive files to an associated file system.
    /// </summary>
    public class FileSyncManager : BaseSyncManager
    {
        private List<Task> _tasks = new List<Task>();
        private int _totalFiles;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSyncManager"/> class.
        /// </summary>
        /// <param name="vault"></param>
        public FileSyncManager(VaultConfig vault) : base(vault) { }

        /// <inheritdoc/>
        public override async Task PushFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
            if (!files.Any()) return;
            SystemFile[] backupFiles = files.Where(f => !f.Deleted).ToArray();
            Log.Information($"Backing up {backupFiles.Length} files...");
            await FileSystem.UploadFilesAsync(backupFiles, progress);
            for (int i = 0; i < files.Length; i++)
            {
                SystemFile file = files.ElementAt(i);
                if (file.Deleted)
                {
                    progress.Report(ProgressOperation.Archiving, file, i, files.Length);
                    await Database.AddHistoryAsync(file.LocalPath, HistoryType.Archived);
                    await Database.AddFileAsync(file);
                }
                else
                {
                    progress.Report(ProgressOperation.Syncing, file, i, files.Length);
                    SystemFile remote = await FileSystem.GetFileAsync(file.RemotePath);
                    if (remote is not null)
                    {
                        file.RemoteSize = remote.RemoteSize;
                        await Database.AddHistoryAsync(file.LocalPath, HistoryType.Pushed);
                        await Database.AddFileAsync(file);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override async Task PullFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
            SystemFile[] restoreFiles = files.Where(f => f.Deleted).ToArray();

            if (!restoreFiles.Any()) return;
            await FileSystem.DownloadFilesAsync(restoreFiles, progress);

            for (int i = 0; i < files.Length; i++)
            {
                SystemFile file = files[i];
                Log.Information($"Restoring file: {file.LocalPath}...");
                file.RemotePath = PathBuilder.Remote(file.LocalPath, Vault.FileSystem);
            }
        }
    }
}