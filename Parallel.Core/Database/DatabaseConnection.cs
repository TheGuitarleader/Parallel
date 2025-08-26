// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Settings;

namespace Parallel.Core.Database
{
    /// <summary>
    /// The supported file service types.
    /// </summary>
    public enum DatabaseProvider
    {
        Local
    }

    /// <summary>
    /// Represents a way to connect to different <see cref="IDatabase"/>.
    /// </summary>
    public class DatabaseConnection
    {
        public static IDatabase CreateNew(VaultConfig vault)
        {
            switch(vault.Database.Provider)
            {
                default: return null;

                case DatabaseProvider.Local:
                    IDatabase db = new SqliteContext(vault.Database, vault.Id);
                    if (!File.Exists(vault.Database.Address)) db.InitializeAsync();
                    return db;
            }
        }
    }
}