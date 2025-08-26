// Copyright 2025 Kyle Ebbinga

using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Parallel.Core.Database;
using Parallel.Core.IO.FileSystem;
using Parallel.Core.Security;
using Parallel.Core.Utils;

namespace Parallel.Core.Settings
{
    /// <summary>
    /// Represents a back-up connection.
    /// </summary>
    public class VaultConfig
    {
        /// <summary>
        /// A unique hash used to identify the vault.
        /// </summary>
        public string Id { get; } = HashGenerator.GenerateHash(12, true);

        /// <summary>
        /// The name of the vault.
        /// </summary>
        public string Name { get; set; } = "Default";

        /// <summary>
        /// The credentials needed to log in to the associated <see cref="IDatabase"/>.
        /// </summary>
        public DatabaseCredentials Database { get; }

        /// <summary>
        /// The credentials needed to log in to the associated <see cref="IFileSystem"/>.
        /// </summary>
        public FileSystemCredentials FileSystem { get; }

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
        public HashSet<string> PruneDirectories { get; } = new HashSet<string>();


        /// <summary>
        /// Initializes a new instance of the <see cref="VaultConfig"/> class.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="database"></param>
        /// <param name="fileSystem"></param>
        [JsonConstructor]
        public VaultConfig(string id, string name, DatabaseCredentials database, FileSystemCredentials fileSystem)
        {
            Id = id;
            Name = name;
            Database = database;
            FileSystem = fileSystem;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="VaultConfig"/> class.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="database"></param>
        /// <param name="fileSystem"></param>
        public VaultConfig(string name, DatabaseCredentials database, FileSystemCredentials fileSystem)
        {
            Id = HashGenerator.GenerateHash(12, true);
            Name = name;
            Database = database;
            FileSystem = fileSystem;
        }

        /// <summary>
        /// Loads settings from a file.
        /// </summary>
        public static VaultConfig? Load(string path)
        {
            if (!File.Exists(path)) return null;
            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<VaultConfig>(json);
        }

        /// <summary>
        /// Loads credentials from the app configuration.
        /// </summary>
        /// <returns>A <see cref="VaultConfig"/> instance.</returns>
        public static VaultConfig? Load(ParallelSettings settings, string name)
        {
            VaultConfig? vault = Load(settings.Vaults.First());
            return string.IsNullOrEmpty(name) ? vault : Load(Path.Combine(ParallelSettings.VaultsDir, name + ".json"));
        }

        /// <summary>
        /// Saves credentials to a file.
        /// </summary>
        /// <param name="vault">The current vault to save.</param>
        public static void Save(VaultConfig vault)
        {
            if (!Directory.Exists(ParallelSettings.VaultsDir)) Directory.CreateDirectory(ParallelSettings.VaultsDir);
            string path = Path.Combine(ParallelSettings.VaultsDir, vault.Name + ".json");
            Log.Debug($"Saving vault file: {path}");
            if (!File.Exists(path))
            {
                Log.Debug("Creating file -> " + path);
                File.Create(path).Close();
            }

            File.WriteAllText(path, JsonConvert.SerializeObject(vault, Formatting.Indented));
        }

        /// <summary>
        /// Saves the current instance to a file.
        /// </summary>
        public void SaveToFile()
        {
            Save(this);
        }

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