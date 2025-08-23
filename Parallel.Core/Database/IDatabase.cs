// Copyright 2025 Kyle Ebbinga

using Parallel.Core.IO;
using System;
using System.Data;
using Parallel.Core.IO.FileSystem;
using Parallel.Core.Models;

namespace Parallel.Core.Database
{
    /// <summary>
    /// The type of history to log.
    /// </summary>
    public enum HistoryType
    {
        /// <summary>
        /// A file that has been deleted locally but is still backed up. 
        /// </summary>
        Archived,

        /// <summary>
        /// A file that has been deleted locally.
        /// </summary>
        Cleaned,

        /// <summary>
        /// A file that has been copied to another location.
        /// </summary>
        Cloned,

        /// <summary>
        /// A file that has been deleted from the backup.
        /// </summary>
        Pruned,

        /// <summary>
        /// A file that was deleted and has been restored.
        /// </summary>
        Restored,

        /// <summary>
        /// A newly synced file.
        /// </summary>
        Synced
    }

    /// <summary>
    /// An interface for interacting with client data storage.
    /// </summary>
    public interface IDatabase
    {
        /// <summary>
        /// The identifier to the profile for this database.
        /// </summary>
        string ProfileId { get; }

        #region Base

        /// <summary>
        /// Creates a new <see cref="IDbConnection"/>.
        /// </summary>
        /// <returns></returns>
        IDbConnection CreateConnection();

        /// <summary>
        /// Called when initializing a new <see cref="IDatabase"/> instance.
        /// <para>Used for creating new table schemas.</para>
        /// </summary>
        Task InitializeAsync();

        #endregion

        #region Files

        Task<bool> AddFileAsync(SystemFile file);

        #endregion

        #region History

        Task<bool> AddHistoryAsync(string path, HistoryType type);

        #endregion

        Task<IEnumerable<SystemFile>> GetFilesAsync(string path, bool b);
    }
}