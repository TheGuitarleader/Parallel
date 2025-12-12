// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Diagnostics;
using Parallel.Core.IO.FileSystem;
using Parallel.Core.Security;
using Parallel.Core.Storage;

namespace Parallel.Core.IO.Blobs
{
    /// <summary>
    /// Represents the way to chunk files into blobs for syncing.
    /// </summary>
    public abstract class BlobStorage
    {
        /// <summary>
        /// The size, in bytes, to use for chunks of a file.
        /// </summary>
        private static readonly int ChunkSize = 4194304;

        /// <summary>
        /// Chunks a file into hashes for blob storage.
        /// </summary>
        /// <param name="sourcePath">The source path of the file.</param>
        /// <param name="tempObjDir">The temp directory to send chunked objects to.</param>
        /// <param name="progress"></param>
        /// <returns></returns>
        public static async Task<FileManifest> CreateManifestAsync(IStorageProvider fileSystem, string sourcePath, string tempObjDir, IProgressReporter progress)
        {
            List<string> chunkHashes = new List<string>();
            await using FileStream fs = File.OpenRead(sourcePath);
            byte[] buffer = new byte[ChunkSize];
            int bytesRead = 0;

            while ((bytesRead = await fs.ReadAsync(buffer)) > 0)
            {
                byte[] chunkData = new byte[bytesRead];
                Buffer.BlockCopy(buffer, 0, chunkData, 0, bytesRead);

                string hash = HashGenerator.CreateSHA256(chunkData);
                string chunkPath = PathBuilder.GetObjectPath(tempObjDir, hash);

                if(!File.Exists(chunkPath)) await File.WriteAllBytesAsync(chunkPath, chunkData);
                chunkHashes.Add(hash);
            }

            Log.Debug($"Wrote {chunkHashes.Count} hashes to {tempObjDir}");
            return new FileManifest(sourcePath, chunkHashes, new FileInfo(sourcePath).Length);
        }

        /// <summary>
        /// Assembles a file from the chunked hashes.
        /// </summary>
        /// <param name="hashes"></param>
        /// <param name="sourcePath">The path to the chunked objects' folder.</param>
        /// <param name="createFilePath"></param>
        public async Task AssembleFileAsync(IEnumerable<string> chunkHashes, string sourcePath, string createFilePath)
        {
            Log.Debug($"Assembling '{createFilePath}' from {chunkHashes.Count()} hashes.");
            await using FileStream createStream = File.Create(createFilePath);
            foreach (string hash in chunkHashes)
            {
                string chunkPath = PathBuilder.GetObjectPath(sourcePath, hash);
                if(!File.Exists(chunkPath)) throw new FileNotFoundException($"Missing chunk for hash: {hash}");

                await using FileStream chunkStream = File.OpenRead(chunkPath);
                await chunkStream.CopyToAsync(createStream);
            }

            await createStream.FlushAsync();
        }
    }
}