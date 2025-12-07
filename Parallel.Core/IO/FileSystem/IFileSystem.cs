// Copyright 2025 Kyle Ebbinga

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parallel.Core.Diagnostics;
using Parallel.Core.Models;

namespace Parallel.Core.IO.FileSystem
{
    /// <summary>
    /// Defines the way for communicating with a file system.
    /// </summary>
    public interface IFileSystem
    {
        /// <summary>
        /// Creates all directories and subdirectories in the specified path unless they already exist.
        /// </summary>
        /// <param name="path"></param>
        Task CreateDirectoryAsync(string path);

        /// <summary>
        /// Deletes the specified directory.
        /// </summary>
        /// <param name="path"></param>
        Task DeleteDirectoryAsync(string path);

        /// <summary>
        /// Deletes the specified file.
        /// </summary>
        /// <param name="path"></param>
        Task DeleteFileAsync(string path);

        /// <summary>
        /// Downloads a file from the associated file system.
        /// </summary>
        /// <param name="files"></param>
        /// <param name="progress"></param>
        Task DownloadFilesAsync(SystemFile[] files, IProgressReporter progress);

        /// <summary>
        /// Returns the parent directory name.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        Task<string> GetDirectoryNameAsync(string path);

        /// <summary>
        /// Gets all the files in the backup.
        /// </summary>
        /// <returns>A dictionary of <see cref="KeyValuePair"/>s with the key being the backup path adn the value being the associated <see cref="SystemFile"/>.</returns>
        Task<Dictionary<string, SystemFile>> GetFilesAsync();

        /// <summary>
        /// Gets all the files in the current directory.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>A read-only collection of <see cref="SystemFile"/>s.</returns>
        Task<SystemFile[]> GetFilesAsync(string path);

        /// <summary>
        /// Gets a file on the associated file system.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        Task<SystemFile> GetFileAsync(string path);

        /// <summary>
        /// Pings the remote file system.
        /// </summary>
        /// <returns>The time, in milliseconds, of the database latency. -1 if disconnected.</returns>
        Task<long> PingAsync();

        /// <summary>
        /// Uploads a file to the associated file system.
        /// </summary>
        /// <param name="files"></param>
        /// <param name="progress"></param>
        Task UploadFilesAsync(SystemFile[] files, IProgressReporter progress);
    }
}