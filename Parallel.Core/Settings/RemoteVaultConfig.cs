// Copyright 2025 Kyle Ebbinga

using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Parallel.Core.Database;
using Parallel.Core.IO.FileSystem;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Security;
using Parallel.Core.Utils;

namespace Parallel.Core.Settings
{
    /// <summary>
    /// Represents the configuration for the vault.
    /// </summary>
    public class RemoteVaultConfig : LocalVaultConfig
    {
        /// <summary>
        /// The amount of time, in minutes, between backup cycles.
        /// <para>Default: 60 minutes</para>
        /// </summary>
        public int BackupInterval { get; set; } = 60;

        /// <summary>
        /// The amount of time, in days, to hold a file before it can be cleaned.
        /// <para>Default: 90 days</para>
        /// </summary>
        public int RetentionPeriod { get; set; } = 90;

        /// <summary>
        /// The amount of time, in days, to hold a file before it can be pruned.
        /// <para>Default: 180 days (6 months)</para>
        /// </summary>
        public int PrunePeriod { get; set; } = 180;

        /// <summary>
        /// A collection of directories to be backed up.
        /// <para>Default: Empty</para>
        /// </summary>
        public HashSet<string> BackupDirectories { get; } = CreateBackupDirectories();

        /// <summary>
        /// A collection of directories to be ignored when archiving or cleaning.
        /// <para>Default: Empty</para>
        /// </summary>
        public HashSet<string> IgnoreDirectories { get; } = CreateIgnoreDirectories();

        /// <summary>
        /// A collection of directories to be cleaned on the machine.
        /// <para>It's important to note that when using the service host this will delete any available file.</para>
        /// <para>Default: Empty</para>
        /// </summary>
        public HashSet<string> CleanDirectories { get; } = CreateCleanDirectories();

        /// <summary>
        /// A collection of deleted directories allowed to be pruned.
        /// <para>Recommended when using a cloud-based <see cref="FileService"/> to save on storage costs.</para>
        /// <para>Default: Empty</para>
        /// </summary>
        public HashSet<string> PruneDirectories { get; } = [];

        public RemoteVaultConfig(LocalVaultConfig localVault) : base(localVault.Id, localVault.Name, localVault.FileSystem) { }

        public RemoteVaultConfig(string profileName, FileSystemCredentials fsc) : base(profileName, fsc) { }

        #region Privates

        private static HashSet<string> CreateBackupDirectories()
        {
            return
            [
                Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
                Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
                Environment.GetFolderPath(Environment.SpecialFolder.MyVideos)
            ];
        }

        private static HashSet<string> CreateIgnoreDirectories()
        {
            HashSet<string> list = new HashSet<string>();

            // Ignore folders on Windows machines
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                list.Add("$RECYCLE.BIN/"); // For NTFS file systems
                list.Add("*.lnk"); // Shortcuts to other paths
            }

            // Ignore folders on Linux machines
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                list.Add("lost+found/"); // File system recovery directory
                list.Add(".Trash/"); // User's trash folder
                list.Add("*.desktop"); // Linux shortcuts
            }

            // Ignore folders on Apple machines
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                list.Add(".Trash/"); // User's trash folder
                list.Add("*.DS_Store"); // macOS Finder metadata
            }

            return list;
        }

        private static HashSet<string> CreateCleanDirectories()
        {
            return
            [
                Path.GetTempPath(),
            ];
        }

        #endregion
    }
}