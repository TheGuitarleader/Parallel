// Copyright 2025 Kyle Ebbinga

using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Settings;
using Parallel.Core.Utils;

namespace Parallel.Core.Database
{
    /// <inheritdoc />
    public class SqliteDatabase : IDatabase
    {
        public string FilePath { get; }
        public string ProfileId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteDatabase"/> class.
        /// </summary>
        /// <param name="credentials"></param>
        /// <param name="profileId"></param>
        public SqliteDatabase(DatabaseCredentials credentials, string profileId)
        {
            FilePath = credentials.Address;
            ProfileId = profileId;
        }

        #region Base

        /// <inheritdoc/>
        public void Initialize()
        {
            Log.Information("Creating local database...");
            File.Create(FilePath).Close();
            File.SetAttributes(FilePath, File.GetAttributes(FilePath) | FileAttributes.Hidden);

            // Create tables
            RunQuery(
                "CREATE TABLE IF NOT EXISTS `files` (`profile` TEXT NOT NULL, `id` TEXT NOT NULL, `name` TEXT NOT NULL, `localpath` TEXT NOT NULL, `remotepath` TEXT NOT NULL, `lastwrite` LONG INTEGER NOT NULL, `lastupdate` LONG INTEGER NOT NULL, `LocalSize` LONG INTEGER NOT NULL, `RemoteSize` LONG INTEGER NOT NULL, `type` TEXT NOT NULL DEFAULT Other CHECK(`type` IN ('Document', 'Photo', 'Music', 'Video', 'Other')), `hidden` INTEGER NOT NULL DEFAULT 0, `readonly` INTEGER NOT NULL DEFAULT 0, `deleted` INTEGER NOT NULL DEFAULT 0, PRIMARY KEY(`profile`, `id`));");

            RunQuery("CREATE TABLE IF NOT EXISTS `history` (`profile` TEXT NOT NULL, `timestamp` LONG INTEGER NOT NULL, `id` TEXT NOT NULL, `path` TEXT NOT NULL, `type` TEXT NOT NULL, PRIMARY KEY(`profile`, `timestamp`));");
        }

        /// <inheritdoc/>
        public DataTable GetQuery(string query)
        {
            DataTable dt = new();
            Log.Debug($"Getting SQL query: {query}");
            using (SqliteConnection sqlite = new("Data Source=" + FilePath))
            using (SqliteCommand cmd = new(query, sqlite))
            {
                sqlite.Open();
                using (SqliteDataReader reader = cmd.ExecuteReader())
                {
                    dt.Load(reader);
                }

                sqlite.Close();
                sqlite.Dispose();
            }

            return dt;
        }

        /// <inheritdoc/>
        public int RunQuery(string query)
        {
            Log.Debug($"Running SQL query: {query}");
            using (SqliteConnection sqlite = new("Data Source=" + FilePath))
            {
                sqlite.Open();
                using (SqliteCommand cmd = sqlite.CreateCommand())
                {
                    cmd.CommandText = query;
                    cmd.CommandType = CommandType.Text;
                    return cmd.ExecuteNonQuery();
                }
            }
        }

        /// <inheritdoc/>
        public long Ping()
        {
            try
            {
                Stopwatch sw = Stopwatch.StartNew();
                using (SqliteConnection sqlite = new("Data Source=" + FilePath))
                {
                    sqlite.Open();
                    using (SqliteCommand cmd = sqlite.CreateCommand())
                    {
                        cmd.CommandText = "SELECT 1;";
                        cmd.CommandType = CommandType.Text;
                        cmd.ExecuteNonQuery();
                    }

                    sqlite.Close();
                }

                return sw.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                Log.Error(ex.GetBaseException().ToString());
                return -1;
            }
        }

        #endregion

        #region Files

        /// <inheritdoc/>
        public bool AddFile(SystemFile file)
        {
            return RunQuery(
                       $"INSERT OR REPLACE INTO files (profile, id, name, localpath, remotepath, lastwrite, lastupdate, LocalSize, RemoteSize, type, hidden, readonly, deleted) VALUES(\"{ProfileId}\", \"{file.Id}\", \"{file.Name}\", \"{file.LocalPath}\", \"{file.RemotePath}\", {file.LastWrite.TotalMilliseconds}, {file.LastUpdate.TotalMilliseconds}, {file.LocalSize}, {file.RemoteSize}, \"{file.Type.ToString()}\", {Converter.ToInt32(file.Hidden)}, {Converter.ToInt32(file.ReadOnly)}, {Converter.ToInt32(file.Deleted)});") >
                   0;
        }

        /// <inheritdoc/>
        public void RemoveFile(SystemFile file)
        {
            RunQuery($"DELETE FROM files WHERE profile = \"{ProfileId}\" AND localpath = \"{file.LocalPath}\" AND remotepath = \"{file.RemotePath}\"");
        }

        /// <inheritdoc/>
        public DataTable GetFiles()
        {
            return GetQuery($"SELECT * FROM files WHERE profile = \"{ProfileId}\" ORDER BY lastupdate DESC");
        }

        /// <inheritdoc/>
        public DataTable GetFiles(bool deleted)
        {
            return GetQuery($"SELECT * FROM files WHERE profile = \"{ProfileId}\" AND deleted = {Converter.ToInt32(deleted)} ORDER BY lastupdate DESC");
        }

        /// <inheritdoc/>
        public DataTable GetFiles(string query, bool deleted)
        {
            return GetQuery($"SELECT * FROM files WHERE profile = \"{ProfileId}\" AND localpath LIKE \"%{query}%\" AND deleted = {Converter.ToInt32(deleted)} ORDER BY lastupdate ASC");
        }

        /// <inheritdoc/>
        public DataTable GetFiles(long milliseconds, bool deleted, int limit)
        {
            return GetQuery($"SELECT * FROM files WHERE profile = \"{ProfileId}\" AND lastupdate <= {milliseconds} AND deleted = {Converter.ToInt32(deleted)} ORDER BY lastupdate ASC LIMIT {limit}");
        }

        /// <inheritdoc/>
        public DataTable GetFiles(string query, long milliseconds, bool deleted, int limit)
        {
            return GetQuery($"SELECT * FROM files WHERE profile = \"{ProfileId}\" AND localpath LIKE \"%{query}%\" AND lastupdate <= {milliseconds} AND deleted = {Converter.ToInt32(deleted)} ORDER BY lastupdate ASC LIMIT {limit}");
        }

        /// <inheritdoc/>
        public SystemFile GetFile(string query)
        {
            DataTable dt = GetQuery($"SELECT * FROM files WHERE profile = \"{ProfileId}\" AND localpath LIKE \"%{query}%\" OR remotepath LIKE \"%{query}%\"");
            if (dt.Rows.Count == 0) return null;
            return new SystemFile(dt.Rows[0]);
        }

        /// <inheritdoc/>
        public int GetTotalFiles(bool deleted)
        {
            DataTable dt = GetQuery($"SELECT COUNT(*) as total FROM files WHERE profile = \"{ProfileId}\" AND  deleted = {Converter.ToInt32(deleted)}");
            if (dt.Rows.Count == 0) return 0;
            return Convert.ToInt32(dt.Rows[0].Field<object>("total"));
        }

        /// <inheritdoc/>
        public long GetLocalSize()
        {
            DataTable dt = GetQuery($"SELECT SUM(LocalSize) as LocalSize FROM files WHERE profile = \"{ProfileId}\"");
            if (dt.Rows.Count == 0) return 0;
            return Convert.ToInt64(dt.Rows[0].Field<object>("LocalSize"));
        }

        /// <inheritdoc/>
        public long GetRemoteSize()
        {
            DataTable dt = GetQuery($"SELECT SUM(RemoteSize) as RemoteSize FROM files WHERE profile = \"{ProfileId}\"");
            if (dt.Rows.Count == 0) return 0;
            return Convert.ToInt64(dt.Rows[0].Field<object>("RemoteSize"));
        }

        #endregion

        #region History

        /// <inheritdoc/>
        public int AddHistory(string path, HistoryType type)
        {
            return RunQuery($"INSERT OR REPLACE INTO history (profile, timestamp, name, path, type) VALUES(\"{ProfileId}\", {UnixTime.Now.TotalMilliseconds}, \"{Path.GetFileName(path)}\", \"{path}\", \"{type.ToString()}\");");
        }

        /// <inheritdoc/>
        public DataTable GetHistory(string query, int limit)
        {
            return GetQuery($"SELECT * FROM history WHERE profile = \"{ProfileId}\" AND path LIKE \"%{query}%\" ORDER BY timestamp DESC LIMIT {limit}");
        }

        /// <inheritdoc/>
        public DataTable GetHistory(string query, HistoryType type, int limit)
        {
            return GetQuery($"SELECT * FROM history WHERE profile = \"{ProfileId}\" AND path LIKE \"%{query}%\" AND type = \"{type.ToString()}\" ORDER BY timestamp DESC LIMIT {limit}");
        }

        #endregion
    }
}