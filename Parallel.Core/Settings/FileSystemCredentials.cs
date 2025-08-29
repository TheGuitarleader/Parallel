// Copyright 2025 Kyle Ebbinga

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Parallel.Core.IO.FileSystem;

namespace Parallel.Core.Settings
{
    /// <summary>
    /// Represents credentials used to gain access to various <see cref="IFileSystem"/>s.
    /// </summary>
    public class FileSystemCredentials
    {
        public FileService Service { get; set; } = FileService.Local;
        public string RootDirectory { get; set; } = string.Empty;
        public string? Address { get; set; }
        public string? Username { get; set; }
        public string? Password { get; set; }

        /// <summary>
        /// If the file system is encrypting files.
        /// </summary>
        public bool Encrypt { get; set; } = false;

        /// <summary>
        /// The master key used for encryption.
        /// </summary>
        public string? EncryptionKey { get; set; } = null;

        public FileSystemCredentials() { }

        public FileSystemCredentials(string root)
        {
            RootDirectory = root;
        }
    }
}