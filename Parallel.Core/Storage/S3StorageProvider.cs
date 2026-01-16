// Copyright 2026 Kyle Ebbinga

using System.IO.Compression;
using System.IO.Pipelines;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Parallel.Core.Models;
using Parallel.Core.Security;
using Parallel.Core.Settings;
using Parallel.Core.Utils;
using Renci.SshNet.Sftp;

namespace Parallel.Core.Storage
{
    /// <summary>
    /// Represents the wrapper for S3 compatible object storage.
    /// </summary>
    public class S3StorageProvider : IStorageProvider
    {
        private readonly IAmazonS3 _client;
        private readonly string _bucket;

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

            _client = new AmazonS3Client(localVault.Credentials.Username, Encryption.Decode(localVault.Credentials.Password), config);
            _bucket = localVault.Credentials.RootDirectory.ToLower();
        }

        public void Dispose()
        {
            _client.Dispose();
            GC.SuppressFinalize(this);
        }

        public async Task CreateDirectoryAsync(string path)
        {
            if (!path.EndsWith("/")) path += "/";
            await _client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = _bucket,
                Key = path,
                ContentBody = string.Empty
            });
        }

        public Task DeleteDirectoryAsync(string path)
        {
            throw new NotImplementedException();
        }

        public async Task DeleteFileAsync(string path)
        {
            if (!await ExistsAsync(path)) return;
            await _client.DeleteObjectAsync(_bucket, path);
        }

        public async Task DownloadFileAsync(SystemFile file, CancellationToken ct = default)
        {
            using GetObjectResponse? response = await _client.GetObjectAsync(_bucket, file.RemotePath, ct);
            await using FileStream createStream = File.Create(file.LocalPath);
            await using GZipStream gzipStream = new GZipStream(response.ResponseStream, CompressionMode.Decompress);
            await gzipStream.CopyToAsync(createStream, ct);
        }

        public async Task<bool> ExistsAsync(string path)
        {
            try
            {
                await _client.GetObjectMetadataAsync(_bucket, path);
                return true;
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return false;
            }
        }

        public Task<string> GetDirectoryName(string path)
        {
            int index = path.LastIndexOf('/');
            return Task.FromResult(index >= 0 ? path[..index] : string.Empty);
        }

        public async Task<SystemFile?> GetFileAsync(string path)
        {
            try
            {
                if (!await ExistsAsync(path)) return null;
                GetObjectMetadataResponse metadata = await _client.GetObjectMetadataAsync(_bucket, path);
                return new SystemFile(path)
                {
                    RemotePath = path,
                    RemoteSize = metadata.ContentLength
                };
            }
            catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return null;
            }
        }

        public Task CloneFileAsync(string source, string target)
        {
            throw new NotImplementedException();
        }

        public async Task<long> UploadFileAsync(SystemFile file, bool overwrite, CancellationToken ct = default)
        {
            if (!overwrite && await ExistsAsync(file.RemotePath))
            {
                Log.Debug($"Skipping file: {file.RemotePath}");
                return Convert.ToInt64((await GetFileAsync(file.RemotePath))?.RemoteSize);
            }

            await CreateDirectoryAsync(await GetDirectoryName(file.RemotePath));
            Pipe pipe = new Pipe();
            TransferUtility utility = new TransferUtility(_client);
            Task uploadTask = utility.UploadAsync(new TransferUtilityUploadRequest
            {
                BucketName = _bucket,
                Key = file.RemotePath,
                InputStream = pipe.Reader.AsStream(),
                ContentType = "application/octet-stream"
            }, ct);

            long totalBytes = 0;
            await using StreamProgress countingStream = new StreamProgress(pipe.Writer.AsStream(), b => totalBytes = b);
            await using FileStream openStream = File.OpenRead(file.LocalPath);
            await using (GZipStream gzipStream = new(countingStream, CompressionLevel.SmallestSize))
            {
                await openStream.CopyToAsync(gzipStream, ct);
                await gzipStream.FlushAsync(ct);
            }

            await pipe.Writer.CompleteAsync();
            await uploadTask.ConfigureAwait(false);
            return totalBytes;
        }
    }
}