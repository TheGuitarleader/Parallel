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

        public Task DownloadFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public async Task DownloadFileAsync(string sourcePath, string destPath)
        {
            await using FileStream openStream = File.OpenRead(sourcePath);
            await using FileStream createStream = File.Create(destPath);
            await using GZipStream gzipStream = new GZipStream(openStream, CompressionMode.Decompress);
            await gzipStream.CopyToAsync(createStream);
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

        public async Task UploadFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
            await System.Threading.Tasks.Parallel.ForEachAsync(files, new ParallelOptions { MaxDegreeOfParallelism = Environment.ProcessorCount / 2 }, async (file, ct) =>
            {

            });
        }

        /// <inheritdoc />
        public async Task UploadFileAsync(string sourcePath, string destPath)
        {


            if (await ExistsAsync(destPath)) File.SetAttributes(destPath, ~FileAttributes.ReadOnly & File.GetAttributes(destPath));
            string? parent = Path.GetDirectoryName(destPath);
            if (parent != null && !Directory.Exists(parent)) Directory.CreateDirectory(parent);

            await using FileStream openStream = File.OpenRead(sourcePath);
            await using FileStream createStream = File.Create(destPath);
            await using GZipStream gzipStream = new GZipStream(createStream, CompressionLevel.SmallestSize);
            await openStream.CopyToAsync(gzipStream);

            File.SetAttributes(destPath, File.GetAttributes(destPath) | FileAttributes.ReadOnly);
        }
    }
}