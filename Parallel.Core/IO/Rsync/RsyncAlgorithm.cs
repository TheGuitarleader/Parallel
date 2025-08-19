// Copyright 2025 Kyle Ebbinga

using FastRsync.Core;
using FastRsync.Delta;
using FastRsync.Diagnostics;
using FastRsync.Signature;

namespace Parallel.Core.IO.Rsync
{
    /// <summary>
    /// Represents the functions for using the rsync algorithm.
    /// </summary>
    public abstract class RsyncAlgorithm
    {
        private static readonly IProgress<ProgressReport> Logging = new RsyncProgress();
        private static int DeltaSize = 206;

        /// <summary>
        /// Creates a new signature file. A signature file contains checksums for file changes.
        /// </summary>
        /// <param name="originalFilePath">The path to the original version of the file.</param>
        /// <param name="signatureFilePath">The path to the signature of the original file.</param>
        public static async Task CreateSignatureAsync(string originalFilePath, string signatureFilePath)
        {
            SignatureBuilder signatureBuilder = new SignatureBuilder();
            await using FileStream originalStream = new FileStream(originalFilePath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.Read);
            await using FileStream signatureStream = new FileStream(signatureFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            await signatureBuilder.BuildAsync(originalStream, new SignatureWriter(signatureStream));
        }

        /// <summary>
        /// Creates a new delta file. A delta file contains the data that changed in the file.
        /// </summary>
        /// <param name="newFilePath">The path to the new version of the file.</param>
        /// <param name="signatureFilePath">The path to the signature of the original file.</param>
        /// <param name="deltaFilePath">The path to the delta of the changed file data.</param>
        public static async Task CreateDeltaAsync(string newFilePath, string signatureFilePath, string deltaFilePath)
        {
            DeltaBuilder deltaBuilder = new DeltaBuilder();
            await using FileStream newFileStream = new FileStream(newFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await using FileStream signatureStream = new FileStream(signatureFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await using FileStream deltaStream = new FileStream(deltaFilePath, FileMode.Create, FileAccess.Write, FileShare.Read);
            await deltaBuilder.BuildDeltaAsync(newFileStream, new SignatureReader(signatureStream, Logging), new AggregateCopyOperationsDecorator(new BinaryDeltaWriter(deltaStream)));
        }

        /// <summary>
        /// Applies a delta file to
        /// </summary>
        /// <param name="newFilePath">The path to the new version of the file.</param>
        /// <param name="originalFilePath">The path to the original version of the file.</param>
        /// <param name="deltaFilePath">The path to the delta of the changed file data.</param>
        public static async Task ApplyDeltaAsync(string newFilePath, string originalFilePath, string deltaFilePath)
        {
            DeltaApplier deltaApplier = new DeltaApplier();
            await using FileStream originalStream = new FileStream(newFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await using FileStream deltaStream = new FileStream(deltaFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            await using FileStream newFileStream = new FileStream(originalFilePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            await deltaApplier.ApplyAsync(originalStream, new BinaryDeltaReader(deltaStream, Logging), newFileStream);
        }
    }
}