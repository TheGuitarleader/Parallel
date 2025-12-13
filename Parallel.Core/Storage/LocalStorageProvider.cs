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
        public async Task DownloadFileAsync(SystemFile file, IProgressReporter progress, CancellationToken ct = default)
        {
            progress.Report(ProgressOperation.Downloading, file);
            await using FileStream openStream = File.OpenRead(file.RemotePath);
            await using FileStream createStream = File.Create(file.LocalPath);
            await using GZipStream gzipStream = new GZipStream(openStream, CompressionMode.Decompress);
            await gzipStream.CopyToAsync(createStream, ct);
        }

        /// <inheritdoc />
        public Task<long> DownloadStreamAsync(Stream output, string remotePath, CancellationToken ct = default)
        {
            throw new NotImplementedException();
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
        public async Task<SystemFile?> UploadFileAsync(SystemFile file, IProgressReporter progress, CancellationToken ct = default)
        {
            try
            {
                progress.Report(ProgressOperation.Uploading, file);
                if (await ExistsAsync(file.RemotePath)) File.SetAttributes(file.RemotePath, ~FileAttributes.ReadOnly & File.GetAttributes(file.RemotePath));
                await CreateDirectoryAsync(await GetDirectoryName(file.RemotePath));

                await using FileStream openStream = new(file.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 4194304, useAsync: true);
                await using FileStream createStream = new(file.RemotePath, FileMode.Create, FileAccess.Write, FileShare.None, 4194304, useAsync: true);
                await using GZipStream gzipStream = new GZipStream(createStream, CompressionLevel.Optimal);
                await openStream.CopyToAsync(gzipStream, ct);

                File.SetAttributes(file.RemotePath, File.GetAttributes(file.RemotePath) | FileAttributes.ReadOnly);
                file.RemoteSize = createStream.Length;
                return file;
            }
            catch (Exception ex)
            {
                Log.Error(ex.GetBaseException().ToString());
                return null;
            }
        }

        /// <inheritdoc />
        public async Task<long> UploadStreamAsync(Stream input, string remotePath, CancellationToken ct = default)
        {
            await CreateDirectoryAsync(await GetDirectoryName(remotePath));
            await using FileStream createStream = File.Create(remotePath);
            await using GZipStream gzipStream = new(createStream, CompressionLevel.SmallestSize);
            await input.CopyToAsync(gzipStream, ct);

            File.SetAttributes(remotePath, File.GetAttributes(remotePath) | FileAttributes.ReadOnly);
            return createStream.Length;
        }
    }
}