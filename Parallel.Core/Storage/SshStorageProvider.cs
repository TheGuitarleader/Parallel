// Copyright 2026 Kyle Ebbinga

using System.Diagnostics;
using System.IO.Compression;
using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Security;
using Parallel.Core.Settings;
using Renci.SshNet;
using Renci.SshNet.Sftp;
using ZstdSharp;

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
        }

        private void InsureConnection()
        {
            if (!_client.IsConnected) _client.Connect();
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
            InsureConnection();
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

        /// <inheritdoc/>
        public async Task DeleteDirectoryAsync(string path)
        {
            InsureConnection();
            if (await ExistsAsync(path))
            {
                await _client.DeleteDirectoryAsync(path);
            }
        }

        /// <inheritdoc/>
        public async Task DeleteFileAsync(string path)
        {
            InsureConnection();
            if (!await ExistsAsync(path)) return;
            await _client.DeleteAsync(path);
        }

        /// <inheritdoc />
        public async Task DownloadFileAsync(string source, string destination, CancellationToken ct = default)
        {
            InsureConnection();
            await using SftpFileStream openStream = _client.OpenRead(source);
            await using FileStream createStream = File.Create(destination);
            await using ZstdStream zstdStream = new(openStream, ZstdStreamMode.Decompress);
            await zstdStream.CopyToAsync(createStream, ct);
        }

        /// <inheritdoc />
        public async Task<bool> ExistsAsync(string path)
        {
            InsureConnection();
            return await _client.ExistsAsync(path);
        }

        /// <inheritdoc/>
        public Task<string> GetDirectoryName(string path)
        {
            InsureConnection();
            string[] subDirs = path.Split('/');
            return Task.FromResult(string.Join("/", subDirs.Take(subDirs.Length - 1)));
        }

        /// <inheritdoc/>
        public async Task<SystemFile?> GetFileAsync(string path)
        {
            InsureConnection();
            if (!await ExistsAsync(path)) return null;

            ISftpFile sf = _client.Get(path);
            return new SystemFile(sf.Name, sf.Length, sf.LastWriteTime);
        }

        /// <inheritdoc />
        public async Task CloneFileAsync(string source, string target)
        {
            InsureConnection();
            if (await ExistsAsync(source)) _client.ChangePermissions(source, 644);
            _client.RenameFile(source, target);
        }

        /// <inheritdoc />
        public async Task<long> UploadFileAsync(string source, string destination, bool overwrite = false, CancellationToken ct = default)
        {
            InsureConnection();
            if (await ExistsAsync(destination))
            {
                if (!overwrite) return Convert.ToInt64((await GetFileAsync(destination))?.RemoteSize);
                _client.ChangePermissions(destination, 644);
            }

            await CreateDirectoryAsync(await GetDirectoryName(destination));
            await using SftpFileStream createStream = _client.Create(destination);
            await using FileStream openStream = File.OpenRead(source);
            await using ZstdStream zstdStream = new(createStream, ZstdStreamMode.Compress);
            await openStream.CopyToAsync(zstdStream, ct);

            _client.ChangePermissions(destination, 444);
            return createStream.Length;
        }
    }
}