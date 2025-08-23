// Copyright 2025 Kyle Ebbinga

using System.Runtime.InteropServices;
using Parallel.Core.IO.FileSystem;
using Parallel.Core.Models;
using Parallel.Core.Settings;
using Parallel.Core.Utils;

namespace Parallel.Core.IO
{
    /// <summary>
    /// Represents the way to build paths on different operating systems.
    /// </summary>
    public class PathBuilder
    {
        public static string TempDirectory
        {
            get
            {
                string tempFolder = Path.Combine(Path.GetTempPath(), $"parallel_{UnixTime.Now.TotalMilliseconds}");
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
        /// Builds the path for the local file system.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="credentials"></param>
        /// <returns></returns>
        public static string Local(string path, FileSystemCredentials credentials)
        {
            string root = Path.Combine(credentials.RootDirectory, "Parallel", Environment.MachineName);
            string main = path.Replace("/", "\\").Replace(root, string.Empty).Replace(".gz", string.Empty);

            Console.WriteLine(root);
            Console.WriteLine(main);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return main.Substring(1, main.Length - 1).Insert(1, ":");
            }

            return main.Replace(@"\", "/");
        }

        public static string RootDirectory(FileSystemCredentials credentials)
        {
            string root = Path.Combine(credentials.RootDirectory, "Parallel", Environment.MachineName);
            Log.Debug($"Root directory: {root}");
            return credentials.Service switch
            {
                FileService.Local => root,
                FileService.Remote => root.Replace('\\', '/'),
                _ => null
            };
        }

        /// <summary>
        /// Builds the path on the remote <see cref="IFileSystem"/>.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="credentials"></param>
        /// <returns></returns>
        public static string Remote(string path, FileSystemCredentials credentials)
        {
            string root = Path.Combine(credentials.RootDirectory, "Parallel", Environment.MachineName, path.Replace(":", string.Empty)) + ".gz";
            return credentials.Service switch
            {
                FileService.Local => root,
                FileService.Remote => root.Replace('\\', '/'),
                _ => null
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
    }
}