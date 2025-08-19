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
        private readonly FileSystemCredentials _credentials;

        /// <summary>
        /// Represents an <see cref="IFileSystem"/> for interacting with physical machine hardware.
        /// </summary>
        /// <param name="credentials">The credentials to log in with.</param>
        public DotNetFileSystem(FileSystemCredentials credentials)
        {
            _credentials = credentials;
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
            if (File.Exists(path))
            {
                File.SetAttributes(path, ~FileAttributes.ReadOnly & File.GetAttributes(path));
                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(path, UIOption.OnlyErrorDialogs, RecycleOption.SendToRecycleBin);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc/>
        public async Task DownloadFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
            if (!files.Any()) return;
            for (int i = 0; i < files.Length; i++)
            {
                SystemFile file = files[i];
                Stopwatch sw = new Stopwatch();

                progress.Report(ProgressOperation.Downloading, file, i, files.Length);
                await using FileStream createStream = File.Create(file.LocalPath);
                await using FileStream openStream = File.OpenRead(file.RemotePath);
                await using GZipStream gzipStream = new GZipStream(openStream, CompressionMode.Decompress);
                await gzipStream.CopyToAsync(createStream);

                Log.Debug($"Downloaded '{file.LocalPath}' in {sw.ElapsedMilliseconds}ms");
            }
        }

        /// <inheritdoc />
        public Task<string> GetDirectoryNameAsync(string path)
        {
            return Task.FromResult(Path.GetDirectoryName(path));
        }

        /// <inheritdoc/>
        public Task<Dictionary<string, SystemFile>> GetFilesAsync()
        {
            Dictionary<string, SystemFile> files = new Dictionary<string, SystemFile>();
            foreach (string file in Directory.GetFiles(PathBuilder.RootDirectory(_credentials), "*.gz", SearchOption.AllDirectories))
            {
                FileInfo fi = new(file);
                files.Add(fi.FullName, new SystemFile(file)
                {
                    Name = fi.Name,
                    RemotePath = fi.FullName,
                    LastWrite = new UnixTime(fi.LastWriteTime),
                    RemoteSize = fi.Length
                });
            }

            return Task.FromResult(files);
        }

        /// <inheritdoc/>
        public Task<SystemFile[]> GetFilesAsync(string path)
        {
            List<SystemFile> list = new();
            foreach (string file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                FileInfo fi = new(file);
                list.Add(new SystemFile(file)
                {
                    Name = fi.Name,
                    RemotePath = fi.FullName,
                    RemoteSize = fi.Length
                });
            }

            return Task.FromResult(list.ToArray());
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
        public Task<long> PingAsync()
        {
            Stopwatch sw = Stopwatch.StartNew();
            if (!Directory.Exists(PathBuilder.RootDirectory(_credentials))) return Task.FromResult<long>(-1);
            return Task.FromResult(sw.ElapsedMilliseconds);
        }

        /// <inheritdoc/>
        public async Task UploadFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
            if (!files.Any()) return;
            for (int i = 0; i < files.Length; i++)
            {
                Stopwatch sw = new Stopwatch();
                SystemFile file = files[i];
                file.RemotePath = PathBuilder.Remote(file.LocalPath, _credentials);

                progress.Report(ProgressOperation.Uploading, file, i, files.Length);
                if (File.Exists(file.RemotePath)) File.SetAttributes(file.RemotePath, ~FileAttributes.ReadOnly & File.GetAttributes(file.RemotePath));
                string parent = Path.GetDirectoryName(file.RemotePath);
                if (!Directory.Exists(parent)) Directory.CreateDirectory(parent);

                await using FileStream createStream = File.Create(file.RemotePath);
                await using FileStream openStream = File.OpenRead(file.LocalPath);
                await using GZipStream gzipStream = new GZipStream(createStream, CompressionLevel.SmallestSize);
                await openStream.CopyToAsync(gzipStream);

                Log.Debug($"Uploaded '{file.RemotePath}' in {sw.ElapsedMilliseconds}ms");
                File.SetAttributes(file.RemotePath, File.GetAttributes(file.RemotePath) | FileAttributes.ReadOnly);
            }
        }
    }
}