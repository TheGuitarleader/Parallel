// Copyright 2025 Kyle Ebbinga

using System.Diagnostics;
using System.IO.Compression;
using Microsoft.VisualBasic.FileIO;
using Newtonsoft.Json.Linq;
using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Settings;
using Parallel.Core.Utils;
using SearchOption = System.IO.SearchOption;

namespace Parallel.Core.IO.FileSystem
{
    /// <summary>
    /// Represents the wrapper for a default dotnet file system.
    /// </summary>
    public class DotNetFileSystem : IFileSystem
    {
        private readonly LocalVaultConfig _vaultConfig;

        /// <summary>
        /// Represents an <see cref="IFileSystem"/> for interacting with physical machine hardware.
        /// </summary>
        /// <param name="vaultConfig">The vault to use.</param>
        public DotNetFileSystem(LocalVaultConfig vaultConfig)
        {
            _vaultConfig = vaultConfig;
        }

        /// <inheritdoc />
        public void Dispose() { }

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
            if (File.Exists(path))
            {
                File.SetAttributes(path, ~FileAttributes.ReadOnly & File.GetAttributes(path));
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task DownloadFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
            await System.Threading.Tasks.Parallel.ForEachAsync(files, ParallelConfig.Options, async (file, ct) =>
            {
                await using FileStream openStream = File.OpenRead(file.RemotePath);
                await using FileStream createStream = File.Create(file.LocalPath);
                await using GZipStream gzipStream = new GZipStream(openStream, CompressionMode.Decompress);
                await gzipStream.CopyToAsync(createStream, ct);
            });
        }

        /// <inheritdoc />
        public Task<bool> ExistsAsync(string path)
        {
            return Task.FromResult(Directory.Exists(path) || File.Exists(path));
        }

        /// <inheritdoc/>
        public Task<SystemFile> GetFileAsync(string path)
        {
            FileInfo fi = new(path);
            return Task.FromResult(new SystemFile(path)
            {
                Name = fi.Name,
                RemotePath = fi.FullName,
                RemoteSize = fi.Length
            });
        }

        /// <inheritdoc />
        public async Task UploadFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
            await System.Threading.Tasks.Parallel.ForEachAsync(files, ParallelConfig.Options, async (file, ct) =>
            {
                if (await ExistsAsync(file.RemotePath)) File.SetAttributes(file.RemotePath, ~FileAttributes.ReadOnly & File.GetAttributes(file.RemotePath));
                string? parent = Path.GetDirectoryName(file.RemotePath);
                if (parent != null && !Directory.Exists(parent)) Directory.CreateDirectory(parent);

                await using FileStream openStream = File.OpenRead(file.LocalPath);
                await using FileStream createStream = File.Create(file.RemotePath);
                await using GZipStream gzipStream = new GZipStream(createStream, CompressionLevel.SmallestSize);
                await openStream.CopyToAsync(gzipStream, ct);

                File.SetAttributes(file.RemotePath, File.GetAttributes(file.RemotePath) | FileAttributes.ReadOnly);
            });
        }
    }
}