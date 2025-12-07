// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Settings;

namespace Parallel.Core.IO.FileSystem
{
    /// <summary>
    /// The supported file service types.
    /// </summary>
    public enum FileService
    {
        /// <summary>
        /// A local file system either through external or networked drives.
        /// </summary>
        Local,

        /// <summary>
        /// A remote file storage server through secure shell.
        /// </summary>
        Remote,

        /// <summary>
        /// A cloud storage server hosted through Amazon simple storage service.
        /// </summary>
        Cloud,
    };

    /// <summary>
    /// Represents the way to connect to different file system associations. This class cannot be inherited.
    /// </summary>
    public static class FileSystemManager
    {
        /// <summary>
        /// Creates a new file system association.
        /// </summary>
        /// <param name="credentials">The credentials needed for the associated file system.</param>
        public static IFileSystem CreateNew(FileSystemCredentials credentials)
        {
            return credentials?.Service switch
            {
                FileService.Local => new DotNetFileSystem(credentials),
                FileService.Remote => new SftpFileSystem(credentials),
                //FileService.Cloud => new AmazonS3FileSystem(credentials),
                _ => null
            };
        }
    }
}