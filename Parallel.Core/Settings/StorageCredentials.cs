// Copyright 2025 Kyle Ebbinga

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Parallel.Core.Storage;

namespace Parallel.Core.Settings
{
    /// <summary>
    /// Represents credentials used to gain access to various <see cref="IStorageProvider"/>s.
    /// </summary>
    public class StorageCredentials
    {
        /// <summary>
        /// The file service type.
        /// </summary>
        public FileService Service { get; set; } = FileService.Local;

        /// <summary>
        /// The root directory or bucket name when using S3.
        /// </summary>
        public string RootDirectory { get; set; } = string.Empty;

        /// <summary>
        /// The address or endpoint when using S3.
        /// </summary>
        public string? Address { get; set; }

        /// <summary>
        /// The username or access key when using S3.
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// The password or secret key when using S3.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// If the file system is encrypting files.
        /// </summary>
        public bool Encrypt { get; set; } = false;

        /// <summary>
        /// The master key used for encryption.
        /// </summary>
        public string? EncryptionKey { get; set; } = null;

        public bool ForceStyle { get; set; }

        public StorageCredentials() { }

        public StorageCredentials(string root)
        {
            RootDirectory = root;
        }
    }
}