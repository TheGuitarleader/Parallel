// Copyright 2026 Kyle Ebbinga

using Amazon.Runtime;
using Amazon.S3;
using Parallel.Core.Models;
using Parallel.Core.Settings;

namespace Parallel.Core.Storage
{
    /// <summary>
    /// Represents the wrapper for S3 compatible object storage.
    /// </summary>
    public class S3StorageProvider : IStorageProvider
    {
        private readonly IAmazonS3 _client;

        /// <summary>
        /// Represents an <see cref="IStorageProvider"/> for interacting with an S3 provider.
        /// </summary>
        /// <param name="localVault">The credentials to log in with.</param>
        public S3StorageProvider(LocalVaultConfig localVault)
        {
            AmazonS3Config config = new AmazonS3Config()
            {
                ServiceURL = localVault.Credentials.Address,
                ForcePathStyle = localVault.Credentials.ForceStyle
            };

            _client = new AmazonS3Client(localVault.Credentials.Username, localVault.Credentials.Password, config);
        }

        public void Dispose()
        {
            _client.Dispose();
            GC.SuppressFinalize(this);
        }

        public Task CreateDirectoryAsync(string path)
        {
            throw new NotImplementedException();
        }

        public Task DeleteDirectoryAsync(string path)
        {
            throw new NotImplementedException();
        }

        public Task DeleteFileAsync(string path)
        {
            throw new NotImplementedException();
        }

        public Task DownloadFileAsync(SystemFile file, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public Task<bool> ExistsAsync(string path)
        {
            throw new NotImplementedException();
        }

        public Task<string> GetDirectoryName(string path)
        {
            throw new NotImplementedException();
        }

        public Task<SystemFile?> GetFileAsync(string path)
        {
            throw new NotImplementedException();
        }

        public Task CloneFileAsync(string source, string target)
        {
            throw new NotImplementedException();
        }

        public Task<long> UploadFileAsync(SystemFile file, bool overwrite, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }
    }
}