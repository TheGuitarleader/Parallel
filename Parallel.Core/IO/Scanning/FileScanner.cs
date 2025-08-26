// Copyright 2025 Kyle Ebbinga

using System.Data;
using System.Diagnostics;
using System.Text;
using Newtonsoft.Json.Linq;
using Parallel.Core.Database;
using Parallel.Core.IO.Backup;
using Parallel.Core.Models;
using Parallel.Core.Settings;
using Parallel.Core.Utils;
using FileInfo = System.IO.FileInfo;

namespace Parallel.Core.IO.Scanning
{
    /// <summary>
    /// Represents file system scanning.
    /// </summary>
    public class FileScanner
    {
        private readonly VaultConfig _vault;
        private readonly IDatabase _db;

        public FileScanner(VaultConfig vault, IDatabase database)
        {
            _vault = vault;
            _db = database;
        }

        public FileScanner(ISyncManager sync)
        {
            _vault = sync.Vault;
            _db = sync.Database;
        }

        /*/// <summary>
        /// Scans all marked backup locations for file changes.
        /// </summary>
        /// <returns></returns>
        public SystemFile[] GetFileChanges()
        {
            List<SystemFile> scannedFiles = new();
            foreach (string path in _profile.BackupDirectories.ToArray())
            {
                SystemFile[] files = GetFileChanges(path, _profile.IgnoreDirectories.ToArray());
                scannedFiles.AddRange(files);
            }

            Log.Information($"Backing up {scannedFiles.Where(x => !x.Deleted).Count()} files...");
            return scannedFiles.ToArray();
        }*/

        /// <summary>
        /// Scans for file changes in a directory.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="ignoreFolders"></param>
        /// <returns>A list of files that have changed since the last backup.</returns>
        public async Task<SystemFile[]> GetFileChangesAsync(string path, string[] ignoreFolders)
        {
            if (!Directory.Exists(path)) return Array.Empty<SystemFile>();

            List<SystemFile> scannedFiles = new List<SystemFile>();
            HashSet<string> localFiles = FileScanner.GetFiles(path, ".", ignoreFolders).ToHashSet();
            IEnumerable<SystemFile> remoteFiles = await _db.GetFilesAsync(path, false);
            foreach (SystemFile remoteFile in remoteFiles)
            {
                if (File.Exists(remoteFile.LocalPath) && remoteFile.RemotePath != null)
                {
                    SystemFile localFile = new SystemFile(remoteFile.LocalPath);
                    if (IsIgnored(localFile.LocalPath, ignoreFolders))
                    {
                        Log.Debug($"Ignored -> {localFile.LocalPath}");
                        localFile.Deleted = true;
                        scannedFiles.Add(localFile);
                    }
                    else if (HasChanged(localFile, remoteFile))
                    {
                        Log.Debug($"Changed -> {localFile.LocalPath}");
                        scannedFiles.Add(localFile);
                    }

                    localFiles.Remove(localFile.LocalPath);
                }
                else
                {
                    Log.Debug($"Deleted -> {remoteFile.LocalPath}");
                    remoteFile.Deleted = true;
                    scannedFiles.Add(remoteFile);
                }
            }

            Log.Debug($"{localFiles.Count} files are untracked! Adding...");
            foreach (var file in localFiles)
            {
                if (File.Exists(file) && !IsIgnored(file, ignoreFolders))
                {
                    Log.Debug($"Created -> {file}");
                    scannedFiles.Add(new SystemFile(file));
                }
            }

            Log.Debug($"{localFiles.Count} files remaining.");
            Log.Information($"Found {scannedFiles.Count:N0} changes in '{path}'");
            return scannedFiles.ToArray();
        }

        public static bool HasChanged(SystemFile localFile, SystemFile? remoteFile)
        {
            return remoteFile == null || (localFile.LastWrite.TotalMilliseconds > remoteFile.LastWrite.TotalMilliseconds && !localFile.CheckSum.SequenceEqual(remoteFile.CheckSum));
        }


        /// <summary>
        /// Gets the total size, in bytes, of a directory.
        /// </summary>
        /// <param name="path">The path of the directory.</param>
        public static long GetDirectorySize(string path)
        {
            long size = 0;
            DirectoryInfo di = new(path);
            EnumerationOptions options = new()
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true
            };

            foreach (FileInfo fi in di.EnumerateFiles("*", options))
            {
                size += fi.Length;
            }

            return size;
        }

        /// <summary>
        /// Gets an array of empty directories.
        /// </summary>
        /// <param name="path">The root directory to search.</param>
        /// <param name="recursive">If it should search recursively.</param>
        /// <returns>An array of empty directories.</returns>
        public static DirectoryInfo[] GetEmptyDirectories(string path, bool recursive = true)
        {
            List<DirectoryInfo> list = new();
            DirectoryInfo directory = new(path);
            EnumerationOptions options = new()
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = recursive
            };

            foreach (DirectoryInfo di in directory.EnumerateDirectories("*", options))
            {
                Log.Debug($"Checking -> {di.FullName}");
                if (!di.EnumerateFileSystemInfos().Any()) list.Add(di);
            }

            Log.Debug($"Found {list.Count} empty directories");
            return list.ToArray();
        }

        /// <summary>
        /// Gets an array of directories older than a specified time.
        /// <para>This includes all directories outside of Parallel's back up directories.</para>
        /// </summary>
        /// <param name="path">The root directory to search.</param>
        /// <param name="start">The time, as a <see cref="UnixTime"/>.</param>
        /// <param name="recursive">If it should search recursively.</param>
        /// <returns>An array of directories in order of oldest first.</returns>
        public static DirectoryInfo[] GetCleanableDirectories(string path, UnixTime start, bool recursive = true)
        {
            Dictionary<DirectoryInfo, DateTime> list = new();
            DirectoryInfo directory = new(path);
            EnumerationOptions options = new()
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = recursive
            };

            foreach (DirectoryInfo di in directory.EnumerateDirectories("*", options))
            {
                DateTime compare = di.CreationTime > di.LastWriteTime ? di.CreationTime : di.LastWriteTime;
                bool older = start.TotalMilliseconds >= new UnixTime(compare).TotalMilliseconds;
                bool exists = list.Keys.Any(d => di.FullName.StartsWith(d.FullName));
                if (!exists && older) list.Add(di, compare);
            }

            Log.Debug($"Found {list.Count} cleanable directories");
            return list.OrderBy(d => d.Value).ToDictionary().Keys.ToArray();
        }

        public static FileInfo[] GetCleanableFiles(string path, UnixTime start, bool recursive = true)
        {
            Dictionary<FileInfo, DateTime> list = new();
            Log.Debug($"Searching '{path}' for files older than {start.ToString("g")}");
            foreach (string file in GetFiles(path, "*"))
            {
                FileInfo fi = new FileInfo(file);
                DateTime compare = fi.CreationTime > fi.LastWriteTime ? fi.CreationTime : fi.LastWriteTime;
                bool older = start.TotalMilliseconds >= new UnixTime(compare).TotalMilliseconds;
                if (older) list.Add(fi, compare);
            }

            Log.Debug($"Found {list.Count} cleanable files");
            return list.OrderBy(d => d.Value).ToDictionary().Keys.ToArray();
        }

        public static IEnumerable<string> GetFiles(string root, string searchPattern)
        {
            return GetFiles(root, searchPattern, Array.Empty<string>());
        }

        public static IEnumerable<string> GetFiles(string root, string searchPattern, string[] exempt)
        {
            Stack<string> pending = new();
            pending.Push(root);
            while (pending.Count != 0)
            {
                string path = pending.Pop();
                IEnumerable<string> next = null;
                try
                {
                    if (!IsIgnored(path, exempt))
                    {
                        //Log.Debug($"Searching -> {path}");
                        next = Directory.EnumerateFiles(path, searchPattern);
                    }
                    // else
                    // {
                    //     Log.Debug($"Ignored -> {path}");
                    // }
                }
                catch
                {
                    Log.Debug("No file access -> " + path);
                }

                if (next != null && next.Count() != 0)
                {
                    foreach (string file in next) yield return file;
                }

                try
                {
                    next = Directory.EnumerateDirectories(path);
                    foreach (string subdir in next) pending.Push(subdir);
                }
                catch
                {
                    Log.Debug("No folder access -> " + path);
                }
            }
        }

        /// <summary>
        /// Scans a directory for duplicate files with the same name and size.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>A <see cref="KeyValuePair{TKey,TValue}"/> array of duplicate files, in order of most duplicate entries, where the key refers to the filename, and the values are an array of <see cref="SystemFile"/>s in order of oldest to newest.</returns>
        public static Dictionary<string, SystemFile[]> GetDuplicateFiles(string path)
        {
            Dictionary<string, List<SystemFile>> dict = new();
            IEnumerable<string> files = GetFiles(path, "*");
            foreach (string file in files)
            {
                SystemFile entry = new(file);
                if (dict.TryGetValue(entry.Name, out List<SystemFile> value))
                {
                    SystemFile key = value.FirstOrDefault();
                    if (entry.LocalSize.Equals(key.LocalSize))
                    {
                        value.Add(entry);
                    }
                }
                else
                {
                    dict.Add(entry.Name, new List<SystemFile> { entry });
                }
            }

            return dict.Where(kv => kv.Value.Count > 1).OrderByDescending(kv => kv.Value.Count).ToDictionary(k => k.Key, v => v.Value.OrderBy(l => l.LastWrite.TotalMilliseconds).ToArray());
        }

        /// <summary>
        /// Checks if the given path is a directory.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>True if path is a directory, otherwise false.</returns>
        public static bool IsDirectory(string path)
        {
            return Directory.Exists(path) && !File.Exists(path);
        }

        /// <summary>
        /// Checks if the given path is a file.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>True if path is a file, otherwise false.</returns>
        public static bool IsFile(string path)
        {
            return !Directory.Exists(path) && File.Exists(path);
        }

        /// <summary>
        /// Checks if the given path is set to be ignored.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="exempt"></param>
        /// <returns>True if ignored, otherwise false.</returns>
        public static bool IsIgnored(string path, string[] exempt)
        {
            foreach (string entry in exempt)
            {
                if (path.StartsWith(entry))
                {
                    return true;
                }

                if (entry.EndsWith('/'))
                {
                    string[] folders = path.Split('\\');
                    foreach (string dir in folders)
                    {
                        if (dir.ToLower() == entry.Remove(entry.Length - 1, 1).ToLower())
                        {
                            return true;
                        }
                    }
                }

                if (entry.StartsWith('*'))
                {
                    if (path.EndsWith(entry.Replace("*", string.Empty)))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}