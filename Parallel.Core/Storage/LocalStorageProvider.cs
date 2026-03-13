// Copyright 2026 Kyle Ebbinga

using System.IO.Compression;
using Newtonsoft.Json.Linq;
using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Settings;
using Parallel.Core.Utils;
using ZstdSharp;

namespace Parallel.Core.Storage
{
    /// <summary>
    /// Represents the wrapper for a default dotnet file system.
    /// </summary>
    public class LocalStorageProvider// : IStorageProvider
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

        public async Task DownloadFileAsync(string source, string destination, CancellationToken ct = default)
        {
            await using FileStream openStream = File.OpenRead(source);
            await using FileStream createStream = File.Create(destination);
            await using ZstdStream zstdStream = new ZstdStream(openStream, ZstdStreamMode.Decompress);
            await zstdStream.CopyToAsync(createStream, ct);
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
        public Task<LocalFile?> GetFileAsync(string path)
        {
            if (!File.Exists(path)) return Task.FromResult<LocalFile?>(null);

            FileInfo fi = new(path);
            LocalFile file = new LocalFile(path)
            {
                Name = fi.Name,
                RemoteSize = fi.Length
            };

            return Task.FromResult<LocalFile?>(file);
        }

        /// <inheritdoc />
        public async Task CloneFileAsync(string source, string target)
        {
            if (await ExistsAsync(source)) File.SetAttributes(source, ~FileAttributes.ReadOnly & File.GetAttributes(source));
            File.Copy(source, target, true);
        }

        /// <inheritdoc />
        public async Task<long> UploadFileAsync(string source, string destination, bool overwrite = false, CancellationToken ct = default)
        {
            if (await ExistsAsync(destination))
            {
                if (!overwrite) return Convert.ToInt64((await GetFileAsync(destination))?.RemoteSize);
                File.SetAttributes(destination, File.GetAttributes(destination) & ~FileAttributes.ReadOnly);
            }

            await CreateDirectoryAsync(await GetDirectoryName(destination));
            
            long totalBytes = 0;
            await using FileStream createStream = File.Create(destination);
            await using StreamProgress countingStream = new StreamProgress(createStream, b => totalBytes = b);
            await using FileStream openStream = File.OpenRead(source);
            await using (ZstdStream zstdStream = new(countingStream, ZstdStreamMode.Compress))
            {
                await openStream.CopyToAsync(zstdStream, ct);
            }

            File.SetAttributes(destination, File.GetAttributes(destination) | FileAttributes.ReadOnly);
            return totalBytes;
        }
    }
}