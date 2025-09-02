// Copyright 2025 Kyle Ebbinga

using System.Diagnostics;
using Parallel.Core.Settings;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using System.IO.Compression;
using Newtonsoft.Json.Linq;
using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Security;
using Parallel.Core.Utils;

namespace Parallel.Core.IO.FileSystem
{
    /// <summary>
    /// Represents the wrapper for an SFTP file system through SSH.
    /// </summary>
    public class SftpFileSystem : IFileSystem
    {
        private readonly ConnectionInfo _connectionInfo;
        private readonly SftpClient _client;

        /// <summary>
        /// Represents an <see cref="IFileSystem"/> for interacting with an SSH server.
        /// </summary>
        /// <param name="localVault">The credentials to log in with.</param>
        public SftpFileSystem(LocalVaultConfig localVault)
        {
            _connectionInfo = new ConnectionInfo(localVault.FileSystem.Address, localVault.FileSystem.Username, new PasswordAuthenticationMethod(localVault.FileSystem.Username, Encryption.Decode(localVault.FileSystem.Password)));
            _client = new SftpClient(_connectionInfo);
            _client.Connect();
        }


        /// <inheritdoc />
        public void Dispose()
        {
            if (_client.IsConnected) _client.Disconnect();
            _client.Dispose();
        }

        /// <inheritdoc/>
        public async Task CreateDirectoryAsync(string path)
        {
            if (_client.IsConnected)
            {
                string parentDir = string.Empty;
                foreach (string subPath in path.Split('/'))
                {
                    parentDir += $"/{subPath}";
                    if (!await _client.ExistsAsync(parentDir))
                    {
                        await _client.CreateDirectoryAsync(parentDir);
                    }
                }
            }
        }

        /// <inheritdoc/>
        public async Task DeleteDirectoryAsync(string path)
        {
            if (await ExistsAsync(path))
            {
                await _client.DeleteDirectoryAsync(path);
            }
        }

        /// <inheritdoc/>
        public async Task DeleteFileAsync(string path)
        {
            if (await ExistsAsync(path))
            {
                await _client.DeleteAsync(path);
            }
        }

        public Task DownloadFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
            throw new NotImplementedException();
        }

        public async Task<bool> ExistsAsync(string path)
        {
            return _client.IsConnected && await _client.ExistsAsync(path);
        }

        /// <inheritdoc/>
        public async Task<SystemFile> GetFileAsync(string path)
        {
            SystemFile file = new SystemFile(path);
            if (await ExistsAsync(path))
            {
                ISftpFile sf = _client.Get(path);
                file = new SystemFile(sf.FullName)
                {
                    Name = sf.Name,
                    RemoteSize = sf.Length,
                };
            }

            return file;
        }

        /// <inheritdoc />
        public async Task UploadFilesAsync(SystemFile[] files, IProgressReporter progress)
        {
            for (int i = 0; i < files.Length; i++)
            {
                SystemFile file = files[i];
                Stopwatch sw = new Stopwatch();
                progress.Report(ProgressOperation.Uploading, file, i, files.Length);
                if (await _client.ExistsAsync(file.RemotePath)) _client.ChangePermissions(file.RemotePath, 644);

                string parentDir = string.Empty;
                foreach (string subPath in file.RemotePath.Split('/'))
                {
                    parentDir += $"/{subPath}";
                    if (!await _client.ExistsAsync(parentDir))
                    {
                        await _client.CreateDirectoryAsync(parentDir);
                    }
                }

                await using SftpFileStream createStream = _client.Create(file.RemotePath);
                await using FileStream openStream = File.OpenRead(file.LocalPath);
                await using GZipStream gzipStream = new GZipStream(createStream, CompressionLevel.SmallestSize);
                await openStream.CopyToAsync(gzipStream);
                _client.ChangePermissions(file.RemotePath, 444);

                Log.Debug($"Uploaded '{file.RemotePath}' in {sw.ElapsedMilliseconds}ms");
            }
        }
    }
}