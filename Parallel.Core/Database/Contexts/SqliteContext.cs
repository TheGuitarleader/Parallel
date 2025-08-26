// Copyright 2025 Kyle Ebbinga

using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;
using Dapper;
using Parallel.Core.Models;
using Parallel.Core.Settings;
using Parallel.Core.Utils;

namespace Parallel.Core.Database
{
    /// <inheritdoc />
    public class SqliteContext : IDatabase
    {
        public string FilePath { get; }
        public string ProfileId { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteContext"/> class.
        /// </summary>
        /// <param name="credentials"></param>
        /// <param name="profileId"></param>
        public SqliteContext(DatabaseCredentials credentials, string profileId)
        {
            FilePath = credentials.Address;
            ProfileId = profileId;
        }

        #region Base

        /// <inheritdoc />
        public IDbConnection CreateConnection()
        {
            return new SqliteConnection("Data Source=" + FilePath);
        }

        /// <inheritdoc />
        public async Task InitializeAsync()
        {
            Log.Information("Creating local database...");
            File.Create(FilePath).Close();
            File.SetAttributes(FilePath, File.GetAttributes(FilePath) | FileAttributes.Hidden);

            using IDbConnection connection = CreateConnection();
            await connection.ExecuteAsync("CREATE TABLE IF NOT EXISTS `files` (`vault` TEXT NOT NULL, `id` TEXT NOT NULL, `name` TEXT NOT NULL, `localpath` TEXT NOT NULL, `remotepath` TEXT NOT NULL, `lastwrite` LONG INTEGER NOT NULL, `lastupdate` LONG INTEGER NOT NULL, `localsize` LONG INTEGER NOT NULL, `remotesize` LONG INTEGER NOT NULL, `type` TEXT NOT NULL DEFAULT Other CHECK(`type` IN ('Document', 'Photo', 'Music', 'Video', 'Other')), `hidden` INTEGER NOT NULL DEFAULT 0, `readonly` INTEGER NOT NULL DEFAULT 0, `deleted` INTEGER NOT NULL DEFAULT 0, `encrypted` INTEGER NOT NULL DEFAULT 0, `salt` BLOB, `iv` BLOB, `checksum` BLOB, PRIMARY KEY(`vault`, `id`));");
            await connection.ExecuteAsync("CREATE TABLE IF NOT EXISTS `history` (`vault` TEXT NOT NULL, `timestamp` LONG INTEGER NOT NULL, `path` TEXT NOT NULL, `name` TEXT NOT NULL, `type` TEXT NOT NULL, PRIMARY KEY(`vault`, `timestamp`));");
        }

        #endregion

        #region Files

        /// <inheritdoc />
        public async Task<bool> AddFileAsync(SystemFile file)
        {
            using IDbConnection connection = CreateConnection();
            string sql = @"INSERT OR REPLACE INTO files (vault, id, name, localpath, remotepath, lastwrite, lastupdate, LocalSize, RemoteSize, type, hidden, readonly, deleted, encrypted, salt, iv) VALUES (@ProfileId, @Id, @Name, @LocalPath, @RemotePath, @LastWrite, @LastUpdate, @LocalSize, @RemoteSize, @Type, @Hidden, @ReadOnly, @Deleted, @Encrypted, @Salt, @IV);";
            return await connection.ExecuteAsync(sql, new { ProfileId, file.Id, file.Name, file.LocalPath, file.RemotePath, LastWrite = file.LastWrite.TotalMilliseconds, LastUpdate = UnixTime.Now.TotalMilliseconds, file.LocalSize, file.RemoteSize, Type = file.Type.ToString(), file.Hidden, file.ReadOnly, file.Deleted, file.Encrypted, file.Salt, file.IV }) > 0;
        }

        /// <inheritdoc />
        public async Task<IEnumerable<SystemFile>> GetFilesAsync(string path, bool deleted)
        {
            using IDbConnection connection = CreateConnection();
            string sql = $"SELECT * FROM files WHERE vault = \"{ProfileId}\" AND deleted = {deleted} ORDER BY lastupdate DESC";
            return await connection.QueryAsync<SystemFile>(sql);
        }

        /// <inheritdoc />
        public async Task<SystemFile?> GetFileAsync(string path)
        {
            using IDbConnection connection = CreateConnection();
            string sql = $"SELECT * FROM files WHERE vault = \"{ProfileId}\" AND localpath LIKE \"%{path}%\" OR remotepath LIKE \"%{path}%\" ORDER BY lastupdate DESC";
            return await connection.QuerySingleOrDefaultAsync<SystemFile>(sql);
        }

        #endregion

        #region History

        /// <inheritdoc />
        public async Task<bool> AddHistoryAsync(string path, HistoryType type)
        {
            using IDbConnection connection = CreateConnection();
            string sql = @"INSERT OR REPLACE INTO history (vault, timestamp, name, path, type) VALUES(@ProfileId, @Timestamp, @Name, @Path, @Type);";
            return await connection.ExecuteAsync(sql, new { ProfileId, Timestamp = UnixTime.Now.TotalMilliseconds, Name = Path.GetFileName(path), Path = path, Type = type }) > 0;
        }

        #endregion
    }
}