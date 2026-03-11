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
using ZstdSharp;

namespace Parallel.Core.Storage
{
    /// <summary>
    /// Represents the wrapper for S3 compatible object storage.
    /// </summary>
    public class S3StorageProvider : IStorageProvider
    {
        private readonly AmazonS3Client _client;
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

        public async Task DownloadFileAsync(string source, string destination, CancellationToken ct = default)
        {
            using GetObjectResponse? response = await _client.GetObjectAsync(_bucket, source, ct);
            await using FileStream createStream = File.Create(destination);
            await using ZstdStream zstdStream = new ZstdStream(response.ResponseStream, ZstdStreamMode.Decompress);
            await zstdStream.CopyToAsync(createStream, ct);
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

        public async Task<long> UploadFileAsync(string source, string destination, bool overwrite, CancellationToken ct = default)
        {
            if (!overwrite && await ExistsAsync(destination))
            {
                Log.Debug($"Skipping file: {destination}");
                return Convert.ToInt64((await GetFileAsync(destination))?.RemoteSize);
            }

            await CreateDirectoryAsync(await GetDirectoryName(destination));
            Pipe pipe = new Pipe();
            TransferUtility utility = new TransferUtility(_client);
            Task uploadTask = utility.UploadAsync(new TransferUtilityUploadRequest
            {
                BucketName = _bucket,
                Key = destination,
                InputStream = pipe.Reader.AsStream(),
                ContentType = "application/octet-stream"
            }, ct);

            long totalBytes = 0;
            await using StreamProgress countingStream = new StreamProgress(pipe.Writer.AsStream(), b => totalBytes = b);
            await using FileStream openStream = File.OpenRead(source);
            await using (ZstdStream zstdStream = new(countingStream, ZstdStreamMode.Compress))
            {
                await openStream.CopyToAsync(zstdStream, ct);
                await zstdStream.FlushAsync(ct);
            }

            await pipe.Writer.CompleteAsync();
            await uploadTask.ConfigureAwait(false);
            return totalBytes;
        }
    }
}