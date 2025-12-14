// Copyright 2025 Kyle Ebbinga

using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using Parallel.Core.IO.FileSystem;
using Parallel.Core.Models;
using Parallel.Core.Settings;
using Parallel.Core.Storage;
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
                if (OperatingSystem.IsWindows())
                {
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Parallel");
                }

                if (OperatingSystem.IsLinux())
                {
                    return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "parallel");
                }

                if (OperatingSystem.IsMacOS())
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
            return Combine(localVault.Credentials.RootDirectory, "Parallel", localVault.Id);
        }

        /// <summary>
        /// Gets the primary location where files are stored in the vault.
        /// </summary>
        /// <param name="localVault"></param>
        /// <returns></returns>
        public static string GetObjectsDirectory(LocalVaultConfig localVault)
        {
            return Combine(GetRootDirectory(localVault), "objects");
        }

        /// <summary>
        /// Gets the location where snapshots are stored in the vault.
        /// </summary>
        /// <param name="localVault"></param>
        /// <returns></returns>
        public static string GetSnapshotsDirectory(LocalVaultConfig localVault)
        {
            return Combine(GetRootDirectory(localVault), "snapshots");
        }

        /// <summary>
        /// Gets the path to the vault's configuration file.
        /// </summary>
        /// <param name="localVault"></param>
        /// <returns></returns>
        public static string GetConfigurationFile(LocalVaultConfig localVault)
        {
            return Combine(GetRootDirectory(localVault), "config");
        }

        /// <summary>
        /// Gets the path to the vault's database file.
        /// </summary>
        /// <param name="localVault"></param>
        /// <returns></returns>
        public static string GetDatabaseFile(LocalVaultConfig localVault)
        {
            return Combine(GetRootDirectory(localVault), "index");
        }

        /// <summary>
        /// Builds the path on the remote <see cref="IStorageProvider"/>.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="credentials"></param>
        /// <returns></returns>
        public static string Remote(string path, RemoteVaultConfig remoteVaultConfig)
        {
            string root = Path.Combine(remoteVaultConfig.Credentials.RootDirectory, "Parallel", remoteVaultConfig.Id, "Files", path.Replace(":", string.Empty)) + ".gz";
            return remoteVaultConfig.Credentials.Service switch
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

        /// <summary>
        /// Gets the relative path for the hash.
        /// </summary>
        /// <param name="vaultConfig"></param>
        /// <param name="hash"></param>
        /// <returns></returns>
        public static string GetObjectPath(LocalVaultConfig vaultConfig, string hash)
        {
            return Combine(GetObjectsDirectory(vaultConfig), hash.Substring(0, 2), hash.Substring(2, 2), hash);
        }
    }
}