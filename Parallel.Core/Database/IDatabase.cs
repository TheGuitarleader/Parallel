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
        /// A file that has been deleted from the vault.
        /// </summary>
        Pruned,

        /// <summary>
        /// A file that was pulled from the vault.
        /// </summary>
        Pulled,

        /// <summary>
        /// A file that was pushed to the vault.
        /// </summary>
        Pushed
    }

    /// <summary>
    /// An interface for interacting with client data storage.
    /// </summary>
    public interface IDatabase
    {
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

        /// <summary>
        /// Adds a new file or updates an existing one.
        /// </summary>
        /// <param name="file"></param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> AddFileAsync(SystemFile file);

        #endregion

        #region History

        /// <summary>
        /// Adds a new history.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="type"></param>
        /// <returns>True if successful, false otherwise</returns>
        Task<bool> AddHistoryAsync(string path, HistoryType type);

        IEnumerable<HistoryEvent>? GetHistory(string path, int limit);

        IEnumerable<HistoryEvent>? GetHistory(string path, HistoryType type, int limit);

        #endregion

        Task<IEnumerable<SystemFile>> GetFilesAsync(string path, bool deleted);
        Task<SystemFile?> GetFileAsync(string path);
    }
}