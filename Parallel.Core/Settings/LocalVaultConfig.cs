// Copyright 2025 Kyle Ebbinga

using Parallel.Core.IO.FileSystem;
using Parallel.Core.Security;

namespace Parallel.Core.Settings
{
    /// <summary>
    /// Represents a localized vault connection configuration.
    /// </summary>
    public class LocalVaultConfig
    {
        /// <summary>
        /// A unique hash used to identify the vault.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// The name of the vault.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// The credentials needed to log in to the associated <see cref="IStorageProvider"/>.
        /// </summary>
        public FileSystemCredentials FileSystem { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="LocalVaultConfig"/> class.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="fileSystem"></param>
        [JsonConstructor]
        public LocalVaultConfig(string id, string name, FileSystemCredentials fileSystem)
        {
            Id = id;
            Name = name;
            FileSystem = fileSystem;
        }

        public LocalVaultConfig(string name, FileSystemCredentials fileSystem)
        {
            Id = HashGenerator.GenerateHash(8, true);
            Name = name;
            FileSystem = fileSystem;
        }

        /// <summary>
        /// Loads settings from a file.
        /// </summary>
        public static LocalVaultConfig? Load(string path)
        {
            return !File.Exists(path) ? null : JsonConvert.DeserializeObject<LocalVaultConfig>(File.ReadAllText(path));
        }

        /// <summary>
        /// Saves credentials to a file.
        /// </summary>
        /// <param name="config"></param>
        /// <param name="localVault"></param>
        public static void Save(ParallelConfig config, LocalVaultConfig localVault)
        {
            config.Vaults.Add(localVault);
            config.Save();
        }
    }
}