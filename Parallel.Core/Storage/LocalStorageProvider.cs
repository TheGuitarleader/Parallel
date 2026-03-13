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

        public async Task DownloadFileAsync(LocalFile file, string remotePath, CancellationToken ct = default)
        {
            await using FileStream openStream = File.OpenRead(remotePath);
            await using FileStream createStream = File.Create(file.Fullname);
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
        public Task<RemoteFile?> GetFileAsync(string path)
        {
            if (!File.Exists(path)) return Task.FromResult<RemoteFile?>(null);

            FileInfo fi = new(path);
            RemoteFile file = new(fi.Name, path, fi.LastWriteTimeUtc, fi.Length, fi.Name);
            return Task.FromResult<RemoteFile?>(file);
        }

        /// <inheritdoc />
        public async Task CloneFileAsync(string source, string target)
        {
            if (await ExistsAsync(source)) File.SetAttributes(source, ~FileAttributes.ReadOnly & File.GetAttributes(source));
            File.Copy(source, target, true);
        }

        /// <inheritdoc />
        public async Task<RemoteFile?> UploadFileAsync(LocalFile file, string remotePath, bool overwrite = false, CancellationToken ct = default)
        {
            if (await ExistsAsync(remotePath))
            {
                Log.Debug("Skipping file: {RemotePath}", remotePath);
                if (!overwrite) return await GetFileAsync(remotePath);
            }

            string tempPath = remotePath + ".tmp";
            await CreateDirectoryAsync(await GetDirectoryName(remotePath));
            
            long totalBytes = 0;
            await using FileStream createStream = File.Create(tempPath);
            await using HashStream hashStream = new(createStream, b => totalBytes = b);
            await using FileStream openStream = File.OpenRead(file.Fullname);
            await using (ZstdStream zstdStream = new(hashStream, ZstdStreamMode.Compress))
            {
                await openStream.CopyToAsync(zstdStream, ct);
            }

            File.Move(tempPath, remotePath, false);
            File.SetAttributes(remotePath, File.GetAttributes(remotePath) | FileAttributes.ReadOnly);
            
            string remoteChecksum = Convert.ToHexStringLower(hashStream.GetHash());
            Log.Information("Uploaded file: {SourcePath} ({RemoteChecksum})", file.Fullname, remoteChecksum);
            return new RemoteFile(file.Name, file.Fullname, file.LastWrite, file.LastUpdate, totalBytes, remoteChecksum);
        }
    }
}