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
        public string Address { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public FileSystemCredentials() { }

        public FileSystemCredentials(string root)
        {
            RootDirectory = root;
        }
    }
}