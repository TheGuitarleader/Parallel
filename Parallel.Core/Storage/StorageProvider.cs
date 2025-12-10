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
    public static class StorageProvider
    {
        /// <summary>
        /// Creates a new file system association.
        /// </summary>
        /// <param name="vaultConfig">The vault needed for the associated file system.</param>
        public static IStorageProvider CreateNew(LocalVaultConfig vaultConfig)
        {
            return vaultConfig.FileSystem.Service switch
            {
                FileService.Local => new LocalStorageProvider(vaultConfig),
                FileService.Remote => new SshStorageProvider(vaultConfig),
                //FileService.Cloud => new AmazonS3FileSystem(credentials),
                _ => null
            };
        }
    }
}