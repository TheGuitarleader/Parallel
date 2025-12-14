// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Diagnostics;
using Parallel.Core.Models;

namespace Parallel.Core.Storage
{
    /// <summary>
    /// Defines the way for communicating with a storage provider.
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
        /// Downloads a file from the associated storage provider.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="ct"></param>
        Task DownloadFileAsync(SystemFile file, CancellationToken ct = default);

        /// <summary>
        /// Checks if a path exists on the associated storage provider.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="ct"></param>
        /// <returns>True if path exists, otherwise false.</returns>
        Task<bool> ExistsAsync(string path);

        /// <summary>
        /// Gets the specified directory path.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<string> GetDirectoryName(string path);

        /// <summary>
        /// Gets a file on the associated storage provider.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        Task<SystemFile?> GetFileAsync(string path);

        /// <summary>
        /// Uploads a file to the associated storage provider.
        /// </summary>
        /// <param name="file"></param>
        /// <param name="overwrite"></param>
        /// <param name="ct"></param>
        /// <returns>The size, in bytes, of the amount of data transferred. Otherwise, 0.</returns>
        Task<long> UploadFileAsync(SystemFile file, bool overwrite, CancellationToken ct = default);
    }
}