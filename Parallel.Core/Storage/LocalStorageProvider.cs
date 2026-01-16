// Copyright 2025 Kyle Ebbinga

using System.IO.Compression;
using Newtonsoft.Json.Linq;
using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Settings;

namespace Parallel.Core.Storage
{
    /// <summary>
    /// Represents the wrapper for a default dotnet file system.
    /// </summary>
    public class LocalStorageProvider : IStorageProvider
    {
        private readonly LocalVaultConfig _vaultConfig;

        /// <summary>
        /// Represents an <see cref="IStorageProvider"/> for interacting with physical machine hardware.
        /// </summary>
        /// <param name="vaultConfig">The vault to use.</param>
        public LocalStorageProvider(LocalVaultConfig vaultConfig)
        {
            _vaultConfig = vaultConfig;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            GC.SuppressFinalize(this);
        }

        /// <inheritdoc/>
        public Task CreateDirectoryAsync(string path)
        {
            Directory.CreateDirectory(path);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteDirectoryAsync(string path)
        {
            //Directory.Delete(path);
            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public Task DeleteFileAsync(string path)
        {
            if (!File.Exists(path)) return Task.CompletedTask;
            File.SetAttributes(path, ~FileAttributes.ReadOnly & File.GetAttributes(path));
            File.Delete(path);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task DownloadFileAsync(SystemFile file, CancellationToken ct = default)
        {
            await using FileStream openStream = File.OpenRead(file.RemotePath);
            await using FileStream createStream = File.Create(file.LocalPath);
            await using GZipStream gzipStream = new GZipStream(openStream, CompressionMode.Decompress);
            await gzipStream.CopyToAsync(createStream, ct);
        }

        /// <inheritdoc />
        public Task<bool> ExistsAsync(string path)
        {
            return Task.FromResult(Directory.Exists(path) || File.Exists(path));
        }

        /// <inheritdoc />
        public Task<string> GetDirectoryName(string path)
        {
            return Task.FromResult(Path.GetDirectoryName(path) ?? string.Empty);
        }

        /// <inheritdoc/>
        public Task<SystemFile?> GetFileAsync(string path)
        {
            if (!File.Exists(path)) return Task.FromResult<SystemFile?>(null);

            FileInfo fi = new(path);
            SystemFile file = new SystemFile(path)
            {
                Name = fi.Name,
                RemotePath = fi.FullName,
                RemoteSize = fi.Length
            };

            return Task.FromResult<SystemFile?>(file);
        }

        /// <inheritdoc />
        public async Task CloneFileAsync(string source, string target)
        {
            if (await ExistsAsync(source)) File.SetAttributes(source, ~FileAttributes.ReadOnly & File.GetAttributes(source));
            File.Copy(source, target, true);
        }

        /// <inheritdoc />
        public async Task<long> UploadFileAsync(SystemFile file, bool overwrite = false, CancellationToken ct = default)
        {
            if (await ExistsAsync(file.RemotePath))
            {
                if (!overwrite)
                {
                    Log.Debug($"Skipping file: {file.RemotePath}");
                    return Convert.ToInt64((await GetFileAsync(file.RemotePath))?.RemoteSize);
                }

                File.SetAttributes(file.RemotePath, ~FileAttributes.ReadOnly & File.GetAttributes(file.RemotePath));
            }

            await CreateDirectoryAsync(await GetDirectoryName(file.RemotePath));
            await using FileStream openStream = File.OpenRead(file.LocalPath);
            await using FileStream createStream = File.Create(file.RemotePath);
            await using GZipStream gzipStream = new GZipStream(createStream, CompressionLevel.SmallestSize);
            await openStream.CopyToAsync(gzipStream, ct);

            File.SetAttributes(file.RemotePath, File.GetAttributes(file.RemotePath) | FileAttributes.ReadOnly);
            return createStream.Length;
        }
    }
}