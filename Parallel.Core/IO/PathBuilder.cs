// Copyright 2025 Kyle Ebbinga

using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Parallel.Core.IO.FileSystem;
using Parallel.Core.Models;
using Parallel.Core.Settings;
using Parallel.Core.Utils;

namespace Parallel.Core.IO
{
    /// <summary>
    /// Represents the way to build paths on different operating systems. This class cannot be inherited.
    /// </summary>
    public class PathBuilder
    {
        private static readonly Regex DriveLetterRegex = new(@"^[a-zA-Z]:", RegexOptions.Compiled);

        public static string TempDirectory
        {
            get
            {
                string tempFolder = Path.Combine(Path.GetTempPath(), "Parallel");
                Log.Debug($"Temp directory: {tempFolder}");
                if (!Directory.Exists(tempFolder)) Directory.CreateDirectory(tempFolder);
                return tempFolder;
            }
        }

        /// <summary>
        /// Gets the corresponding directory for program data based on the <see cref="OSPlatform"/>.
        /// </summary>
        public static string ProgramData
        {
            get
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Parallel");
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {
                    return "/etc/Parallel";
                }

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Library", "Application Support", "Parallel");
                }

                throw new PlatformNotSupportedException("Unsupported OS detected.");
            }
        }

        /// <summary>
        /// Combines an array of strings into a path. This differs from <see cref="Path.Combine(string,string)"/> by using the string context for combining paths instead of using the path operator environment variable.
        /// </summary>
        /// <param name="paths"></param>
        /// <returns></returns>
        public static string Combine(params string[] paths)
        {
            ArgumentNullException.ThrowIfNull(paths);
            if (paths.Length == 0) return string.Empty;

            // Detect context from the first path
            bool isWindowsStyle = DriveLetterRegex.IsMatch(paths[0]);

            char separator = isWindowsStyle ? '\\' : '/';
            char altSeparator = isWindowsStyle ? '/' : '\\';

            StringBuilder sb = new StringBuilder();
            foreach (string p in paths)
            {
                if (string.IsNullOrWhiteSpace(p)) continue;

                string part = p.Replace(altSeparator, separator);

                if (sb.Length == 0)
                {
                    sb.Append(part.TrimEnd(separator));
                }
                else
                {
                    sb.Append(separator);
                    sb.Append(part.Trim(separator));
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets the root directory of the vault.
        /// </summary>
        /// <param name="localVault"></param>
        /// <returns></returns>
        public static string GetRootDirectory(LocalVaultConfig localVault)
        {
            return Combine(localVault.FileSystem.RootDirectory, "Parallel", localVault.Id);
        }

        /// <summary>
        /// Gets the primary location where files are stored in the vault.
        /// </summary>
        /// <param name="localVault"></param>
        /// <returns></returns>
        public static string GetFilesDirectory(LocalVaultConfig localVault)
        {
            return Combine(GetRootDirectory(localVault), "Files");
        }

        /// <summary>
        /// Gets the location where snapshots are stored in the vault.
        /// </summary>
        /// <param name="localVault"></param>
        /// <returns></returns>
        public static string GetSnapshotsDirectory(LocalVaultConfig localVault)
        {
            return Combine(GetRootDirectory(localVault), "Snapshots");
        }

        /// <summary>
        /// Gets the path to the vault's configuration file.
        /// </summary>
        /// <param name="localVault"></param>
        /// <returns></returns>
        public static string GetConfigurationFile(LocalVaultConfig localVault)
        {
            return Combine(GetRootDirectory(localVault), "config.json.gz");
        }

        /// <summary>
        /// Gets the path to the vault's database file.
        /// </summary>
        /// <param name="localVault"></param>
        /// <returns></returns>
        public static string GetDatabaseFile(LocalVaultConfig localVault)
        {
            return Combine(GetRootDirectory(localVault), "index.db.gz");
        }

        /// <summary>
        /// Builds the path on the remote <see cref="IFileSystem"/>.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="credentials"></param>
        /// <returns></returns>
        public static string Remote(string path, RemoteVaultConfig remoteVaultConfig)
        {
            string root = Path.Combine(remoteVaultConfig.FileSystem.RootDirectory, "Parallel", remoteVaultConfig.Id, "Files", path.Replace(":", string.Empty)) + ".gz";
            return remoteVaultConfig.FileSystem.Service switch
            {
                FileService.Local => root,
                FileService.Remote => root.Replace('\\', '/'),
                _ => string.Empty
            };
        }

        public static bool IsDirectory(string path)
        {
            return Directory.Exists(path) && !File.Exists(path);
        }

        public static bool IsFile(string path)
        {
            return !Directory.Exists(path) && File.Exists(path);
        }

        public static string GetObjectPath(string basePath, string hash, int shards = 3)
        {
            if (hash.Length < shards * 2) throw new ArgumentException("Hash too short for sharding", nameof(hash));
            string parentDir = Path.Combine(basePath, hash.Substring(0, 2), hash.Substring(2, 2), hash.Substring(4, 2), hash.Substring(6, 2));
            if (!Directory.Exists(parentDir))  Directory.CreateDirectory(parentDir);
            return Path.Combine(parentDir, hash);
        }
    }
}