// Copyright 2025 Kyle Ebbinga

using Newtonsoft.Json.Linq;
using Parallel.Core.Database;
using Parallel.Core.Diagnostics;
using Parallel.Core.Events;
using Parallel.Core.IO.FileSystem;
using Parallel.Core.Models;
using Parallel.Core.Settings;

namespace Parallel.Core.IO.Backup
{
    /// <summary>
    /// Represents the way to archive files to an associated file system.
    /// </summary>
    public class FileBackupManager : BaseFileManager
    {
        private List<Task> _tasks = new List<Task>();
        private int _totalFiles;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileBackupManager"/> class.
        /// </summary>
        /// <param name="profile"></param>
        public FileBackupManager(ProfileConfig profile) : base(profile) { }

        /// <inheritdoc/>
        public override async Task BackupFilesAsync(SystemFile[] files, IProgress progress)
        {
            if (!files.Any()) return;
            SystemFile[] backupFiles = files.Where(f => !f.IsDeleted).ToArray();
            Log.Information($"Backing up {backupFiles.Length} files...");
            await FileSystem.UploadFilesAsync(backupFiles, progress);
            for (int i = 0; i < files.Length; i++)
            {
                SystemFile file = files.ElementAt(i);
                if (file.IsDeleted)
                {
                    Database.AddHistory(file.LocalPath, HistoryType.Archived);
                    Database.AddFile(file);
                    progress.Report(ProgressOperation.Archiving, file, i, files.Length);
                }
                else
                {
                    progress.Report(ProgressOperation.Syncing, file, i, files.Length);
                    SystemFile remote = await FileSystem.GetFileAsync(file.RemotePath);
                    if (remote is not null)
                    {
                        file.RemoteSize = remote.RemoteSize;
                        Database.AddHistory(file.LocalPath, HistoryType.Synced);
                        Database.AddFile(file);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public override async Task RestoreFilesAsync(SystemFile[] files, IProgress progress)
        {
            SystemFile[] restoreFiles = files.Where(f => f.IsDeleted).ToArray();

            if (!restoreFiles.Any()) return;
            await FileSystem.DownloadFilesAsync(restoreFiles, progress);


            for (int i = 0; i < files.Length; i++)
            {
                SystemFile file = files[i];
                Log.Information($"Restoring file: {file.LocalPath}...");
                file.RemotePath = PathBuilder.Remote(file.LocalPath, Profile.FileSystem);
            }
        }
    }
}