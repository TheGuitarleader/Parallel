// Copyright 2025 Kyle Ebbinga

using System.Diagnostics;
using System.IO.Compression;
using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Security;
using Parallel.Core.Settings;
using Renci.SshNet;
using Renci.SshNet.Sftp;

namespace Parallel.Core.Storage
{
    /// <summary>
    /// Represents the wrapper for an SFTP file system through SSH.
    /// </summary>
    public class SshStorageProvider : IStorageProvider
    {
        private readonly ConnectionInfo _connectionInfo;
        private readonly SftpClient _client;

        /// <summary>
        /// Represents an <see cref="IStorageProvider"/> for interacting with an SSH server.
        /// </summary>
        /// <param name="localVault">The credentials to log in with.</param>
        public SshStorageProvider(LocalVaultConfig localVault)
        {
            _connectionInfo = new ConnectionInfo(localVault.Credentials.Address, localVault.Credentials.Username, new PasswordAuthenticationMethod(localVault.Credentials.Username, Encryption.Decode(localVault.Credentials.Password)));
            _client = new SftpClient(_connectionInfo);
            _client.Connect();
        }


        /// <inheritdoc />
        public void Dispose()
        {
            if (_client.IsConnected) _client.Disconnect();
            _client.Dispose();
            GC.SuppressFinalize(this);
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

        /// <inheritdoc />
        public async Task DownloadFileAsync(SystemFile file, IProgressReporter progress, CancellationToken ct = default)
        {
            progress.Report(ProgressOperation.Downloading, file);
            await using SftpFileStream openStream = _client.OpenRead(file.RemotePath);
            await using FileStream createStream = File.Create(file.LocalPath);
            await using GZipStream gzipStream = new GZipStream(openStream, CompressionMode.Decompress);
            await gzipStream.CopyToAsync(createStream, ct);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(string path)
        {
            return _client.IsConnected && await _client.ExistsAsync(path);
        }

        /// <inheritdoc/>
        public Task<string> GetDirectoryName(string path)
        {
            string[] subDirs = path.Split('/');
            return Task.FromResult(string.Join("/", subDirs.Take(subDirs.Length - 1)));
        }

        /// <inheritdoc/>
        public async Task<SystemFile?> GetFileAsync(string path)
        {
            if (!await ExistsAsync(path)) return null;

            ISftpFile sf = _client.Get(path);
            return new SystemFile(sf.Name, sf.FullName, sf.Length, sf.LastWriteTime);
        }

        /// <inheritdoc />
        public async Task<SystemFile?> UploadFileAsync(SystemFile file, IProgressReporter progress, CancellationToken ct = default)
        {
            try
            {
                progress.Report(ProgressOperation.Uploading, file);
                _client.ChangePermissions(file.RemotePath, 644);
                await CreateDirectoryAsync(await GetDirectoryName(file.RemotePath));

                await using SftpFileStream createStream = _client.Create(file.RemotePath);
                await using FileStream openStream = File.OpenRead(file.LocalPath);
                await using GZipStream gzipStream = new(createStream, CompressionLevel.Optimal);
                await openStream.CopyToAsync(gzipStream, ct);

                _client.ChangePermissions(file.RemotePath, 444);
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
        public Task<long> DownloadStreamAsync(Stream output, string remotePath, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public async Task<long> UploadStreamAsync(Stream input, string remotePath, CancellationToken ct = default)
        {
            await using SftpFileStream createStream = _client.Create(remotePath);
            await using GZipStream gzipStream = new(createStream, CompressionLevel.SmallestSize);
            await input.CopyToAsync(gzipStream, ct);

            _client.ChangePermissions(remotePath, 444);
            return createStream.Length;
        }
    }
}