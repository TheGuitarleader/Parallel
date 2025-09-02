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
    public interface IFileSystem : IDisposable
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
        /// Downloads an array of files from the associated file system.
        /// </summary>
        /// <param name="files"></param>
        /// <param name="progress"></param>
        Task DownloadFilesAsync(SystemFile[] files, IProgressReporter progress);

        /// <summary>
        /// Checks if a path exists on the associated file system.
        /// </summary>
        /// <param name="path"></param>
        /// <returns>True if path exists, otherwise false.</returns>
        Task<bool> ExistsAsync(string path);

        /// <summary>
        /// Gets a file on the associated file system.
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        Task<SystemFile> GetFileAsync(string path);

        /// <summary>
        /// Uploads an array of files to the associated file system.
        /// </summary>
        /// <param name="files"></param>
        /// <param name="progress"></param>
        Task UploadFilesAsync(SystemFile[] files, IProgressReporter progress);
    }
}