// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Diagnostics;
using Parallel.Core.Models;

namespace Parallel.Core.Storage
{
    /// <summary>
    /// Defines the way for communicating with a file system.
    /// </summary>
    public interface IStorageProvider : IDisposable
    {
        /// <summary>
        /// Creates all directories and subdirectories in the specified path unless they already exist.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="ct"></param>
        Task CreateDirectoryAsync(string path);

        /// <summary>
        /// Deletes the specified directory.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="ct"></param>
        Task DeleteDirectoryAsync(string path);

        /// <summary>
        /// Deletes the specified file.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="ct"></param>
        Task DeleteFileAsync(string path);

        /// <summary>
        /// Downloads a file from the associated file system.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="progress"></param>
        /// <param name="ct"></param>
        Task DownloadFileAsync(SystemFile file, IProgressReporter progress, CancellationToken ct = default);

        /// <summary>
        /// Checks if a path exists on the associated file system.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="ct"></param>
        /// <returns>True if path exists, otherwise false.</returns>
        Task<bool> ExistsAsync(string path);

        /// <summary>
        ///
        /// </summary>
        /// <param name="path"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<string> GetDirectoryName(string path);

        /// <summary>
        /// Gets a file on the associated file system.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<SystemFile?> GetFileAsync(string path);

        /// <summary>
        /// Uploads a stream to the associated file system.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="progress"></param>
        /// <param name="ct"></param>
        Task<SystemFile?> UploadFileAsync(SystemFile file, IProgressReporter progress, CancellationToken ct = default);

        Task<long> DownloadStreamAsync(Stream output, string remotePath, CancellationToken ct = default);
        Task<long> UploadStreamAsync(Stream input, string remotePath, CancellationToken ct = default);
    }
}