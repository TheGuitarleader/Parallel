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
        string ProfileId { get; }

        #region Base

        /// <summary>
        /// Called when initializing a new <see cref="IDatabase"/> instance.
        /// <para>Used for creating new table schemas.</para>
        /// </summary>
        void Initialize();

        /// <summary>
        /// Runs a query in the assigned database language.
        /// </summary>
        /// <param name="query">The query to run.</param>
        /// <returns>The id of the last inserted row. -1 if none exist.</returns>
        int RunQuery(string query);

        /// <summary>
        /// Gets data from a query in the assigned database language and converts it to a <see cref="DataTable"/>.
        /// </summary>
        /// <param name="query">The query to send to the database.</param>
        /// <returns>A <see cref="DataTable"/> filled with returned data.</returns>
        DataTable GetQuery(string query);

        /// <summary>
        /// Pings the database server.
        /// </summary>
        /// <returns>The time, in milliseconds, of the database latency. -1 if disconnected.</returns>
        long Ping();

        #endregion

        #region Files

        /// <summary>
        /// Adds a new file or updates an existing one.
        /// </summary>
        /// <param name="file"></param>
        /// <returns>The id of the last inserted row. -1 if none exist.</returns>
        bool AddFile(SystemFile file);

        /// <summary>
        /// Removes a file.
        /// </summary>
        /// <param name="file"></param>
        void RemoveFile(SystemFile file);

        /// <summary>
        /// Gets a <see cref="DataTable"/> of all files in order of most recent.
        /// </summary>
        DataTable GetFiles();

        /// <summary>
        /// Gets a <see cref="DataTable"/> of files of either local files or deleted files in order of most recent.
        /// </summary>
        /// <param name="deleted">true if the table should only be deleted files, otherwise false</param>
        DataTable GetFiles(bool deleted);

        /// <summary>
        /// Gets a <see cref="DataTable"/> of files of either local files or deleted files by a query in order of most recent.
        /// </summary>
        /// <param name="query">The query to search local file paths for.</param>
        /// <param name="deleted">true if the table should only be deleted files, otherwise false</param>
        DataTable GetFiles(string query, bool deleted);

        /// <summary>
        /// Gets a <see cref="DataTable"/> of files by their last update milliseconds in order of oldest first.
        /// </summary>
        /// <param name="milliseconds">The lowest millisecond value to look for.</param>
        /// <param name="deleted">True if the table should only be deleted files, otherwise false</param>
        /// <param name="limit">The total amount of entries to return.</param>
        DataTable GetFiles(long milliseconds, bool deleted, int limit);

        /// <summary>
        /// Gets a <see cref="DataTable"/> of either local files or deleted files from a query and last update milliseconds in order of oldest first.
        /// </summary>
        /// <param name="query">The path to search local deleted files for.</param>
        /// <param name="milliseconds">The lowest millisecond value to look for.</param>
        /// <param name="deleted">true if the table should only be deleted files, otherwise false</param>
        /// <param name="limit">The total amount of entries to return.</param>
        DataTable GetFiles(string query, long milliseconds, bool deleted, int limit);

        /// <summary>
        /// Gets a specific file in the database.
        /// </summary>
        /// <param name="query">The file path. Either local or on in the backup.</param>
        /// <returns></returns>
        SystemFile GetFile(string query);

        long GetLocalSize();
        long GetRemoteSize();
        int GetTotalFiles(bool deleted);

        #endregion

        #region History

        int AddHistory(string path, HistoryType type);

        DataTable GetHistory(string path, int limit);

        DataTable GetHistory(string path, HistoryType type, int limit);

        #endregion
    }
}