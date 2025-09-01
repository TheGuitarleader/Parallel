// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Database;
using Parallel.Core.IO;

namespace Parallel.Core.Settings
{
    /// <summary>
    /// Represents credentials used to gain access to various <see cref="IDatabase"/>s.
    /// </summary>
    public class DatabaseCredentials
    {
        /// <summary>
        /// The associated provider of this database.
        /// </summary>
        public DatabaseProvider Provider { get; set; } = DatabaseProvider.Local;

        /// <summary>
        /// The hostname or address of the database.
        /// <para>If using a <see cref="SqliteContext"/>, this will be a file path.</para>
        /// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// The username of the database.
        /// </summary>
        public string? Username { get; set; }

        /// <summary>
        /// The password of the database.
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// The database name.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        public static DatabaseCredentials Local => new()
        {
            Provider = DatabaseProvider.Local,
            Address = Path.Combine(PathBuilder.ProgramData, $"{Environment.MachineName}.db")
        };
    }
}