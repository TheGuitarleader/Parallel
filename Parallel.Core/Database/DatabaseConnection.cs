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
        public static IDatabase CreateNew(ProfileConfig profile)
        {
            switch(profile.Database.Provider)
            {
                default: return null;

                case DatabaseProvider.Local:
                    IDatabase db = new SqliteDatabase(profile.Database, profile.Id);
                    if (!File.Exists(profile.Database.Address)) db.Initialize();
                    return db;
            }
        }
    }
}