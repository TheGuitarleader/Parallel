// Copyright 2025 Kyle Ebbinga

using Microsoft.Data.Sqlite;
using System.Data;
using System.Diagnostics;
using Dapper;
using Parallel.Core.IO;
using Parallel.Core.Models;
using Parallel.Core.Settings;
using Parallel.Core.Utils;

namespace Parallel.Core.Database
{
    /// <inheritdoc />
    public class SqliteContext : IDatabase
    {
        private string FilePath { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteContext"/> class.
        /// </summary>
        /// <param name="credentials"></param>
        /// <param name="profileId"></param>
        public SqliteContext(string filePath)
        {
            FilePath = filePath;
        }

        public void Dispose()
        {
            // TODO release managed resources here
        }

        #region Base

        /// <inheritdoc />
        public IDbConnection CreateConnection()
        {
            return new SqliteConnection($"Data Source={FilePath};Pooling=false;");
        }

        /// <inheritdoc />
        public async Task InitializeAsync()
        {
            Log.Information("Creating index database...");

            using IDbConnection connection = CreateConnection();
            await connection.ExecuteAsync("CREATE TABLE IF NOT EXISTS `chunks` (`id` INTEGER PRIMARY KEY AUTOINCREMENT, `manifestId`, `hash` TEXT NOT NULL, order_index INTEGER NOT NULL, FOREIGN KEY (manifestId) REFERENCES manifests(id));");
            await connection.ExecuteAsync("CREATE TABLE IF NOT EXISTS `manifests` (`id` INTEGER AUTOINCREMENT, `name` TEXT NOT NULL, `path` TEXT NOT NULL, `lastwrite` LONG INTEGER NOT NULL, `lastupdate` LONG INTEGER NOT NULL`type` TEXT NOT NULL DEFAULT Other CHECK(`type` IN ('Document', 'Photo', 'Music', 'Video', 'Other')), `hidden` INTEGER NOT NULL DEFAULT 0, `readonly` INTEGER NOT NULL DEFAULT 0, `deleted` INTEGER NOT NULL DEFAULT 0, `checksum` BLOB, PRIMARY KEY (`id`, `path`));");
            await connection.ExecuteAsync("CREATE TABLE IF NOT EXISTS `files` (`id` TEXT NOT NULL, `name` TEXT NOT NULL, `localpath` TEXT NOT NULL, `remotepath` TEXT NOT NULL, `lastwrite` LONG INTEGER NOT NULL, `lastupdate` LONG INTEGER NOT NULL, `localsize` LONG INTEGER NOT NULL, `remotesize` LONG INTEGER NOT NULL, `type` TEXT NOT NULL DEFAULT Other CHECK(`type` IN ('Document', 'Photo', 'Music', 'Video', 'Other')), `hidden` INTEGER NOT NULL DEFAULT 0, `readonly` INTEGER NOT NULL DEFAULT 0, `deleted` INTEGER NOT NULL DEFAULT 0, `checksum` BLOB, PRIMARY KEY(`id`));");
            await connection.ExecuteAsync("CREATE TABLE IF NOT EXISTS `history` (`timestamp` LONG INTEGER NOT NULL, `path` TEXT NOT NULL, `name` TEXT NOT NULL, `type` TEXT NOT NULL, PRIMARY KEY(`timestamp`));");
        }

        #endregion

        #region Chunks

        /// <inheritdoc />
        public async Task<int> AddChunkAsync(int manifestId, string hash, int index)
        {
            using IDbConnection connection = CreateConnection();
            string sql = @"INSERT OR REPLACE INTO chunks (manifestId, hash, order_index) VALUES (@manifestId, @hash, @index);";
            return await connection.ExecuteAsync(sql, new { manifestId, hash, index });
        }

        #endregion

        #region Files

        /// <inheritdoc />
        public async Task<int> AddFileAsync(SystemFile file)
        {
            using IDbConnection connection = CreateConnection();
            string sql = @"INSERT OR REPLACE INTO files (id, name, localpath, remotepath, lastwrite, lastupdate, LocalSize, RemoteSize, type, hidden, readonly, deleted, checksum) VALUES (@Id, @Name, @LocalPath, @RemotePath, @LastWrite, @LastUpdate, @LocalSize, @RemoteSize, @Type, @Hidden, @ReadOnly, @Deleted, @CheckSum);";
            return await connection.ExecuteAsync(sql, new { file.Id, file.Name, file.LocalPath, file.RemotePath, LastWrite = file.LastWrite.TotalMilliseconds, LastUpdate = UnixTime.Now.TotalMilliseconds, file.LocalSize, file.RemoteSize, Type = file.Type.ToString(), file.Hidden, file.ReadOnly, file.Deleted, file.CheckSum });
        }

        public async Task<long> GetLocalSizeAsync()
        {
            using IDbConnection connection = CreateConnection();
            string sql = $"SELECT SUM(localsize) FROM files;";
            return await connection.QuerySingleOrDefaultAsync<long>(sql);
        }

        public async Task<long> GetRemoteSizeAsync()
        {
            using IDbConnection connection = CreateConnection();
            string sql = $"SELECT SUM(remotesize) FROM files;";
            return await connection.QuerySingleOrDefaultAsync<long>(sql);
        }

        public async Task<long> GetTotalFilesAsync(bool deleted)
        {
            using IDbConnection connection = CreateConnection();
            string sql = $"SELECT COUNT(*) FROM files WHERE deleted = @deleted;";
            return await connection.QuerySingleOrDefaultAsync<long>(sql, new { deleted });
        }

        /// <inheritdoc />
        public async Task<IEnumerable<SystemFile>> GetFilesAsync(string path)
        {
            using IDbConnection connection = CreateConnection();
            string sql = $"SELECT * FROM files WHERE localpath LIKE \"%@path%\" OR remotepath LIKE \"%@path%\" ORDER BY lastupdate DESC";
            return await connection.QueryAsync<SystemFile>(sql, new { path });
        }

        /// <inheritdoc />
        public async Task<IEnumerable<SystemFile>> GetFilesAsync(string path, bool deleted)
        {
            using IDbConnection connection = CreateConnection();
            string sql = $"SELECT * FROM files WHERE deleted = @deleted ORDER BY lastupdate DESC";
            return await connection.QueryAsync<SystemFile>(sql,new { deleted });
        }

        /// <inheritdoc />
        public async Task<SystemFile?> GetFileAsync(string path)
        {
            using IDbConnection connection = CreateConnection();
            string sql = $"SELECT (id, name, localpath, remotepath, lastwrite, lastupdate, LocalSize, RemoteSize, type, hidden, readonly, deleted, checksum) FROM files WHERE localpath LIKE \"%{path}%\" OR remotepath LIKE \"%{path}%\" ORDER BY lastupdate DESC";
            return await connection.QuerySingleOrDefaultAsync<SystemFile>(sql);
        }

        #endregion

        #region History

        /// <inheritdoc />
        public async Task<int> AddHistoryAsync(string path, HistoryType type)
        {
            using IDbConnection connection = CreateConnection();
            string sql = @"INSERT OR REPLACE INTO history (timestamp, name, path, type) VALUES(@Timestamp, @Name, @Path, @Type);";
            return await connection.ExecuteAsync(sql, new { Timestamp = UnixTime.Now.TotalMilliseconds, Name = Path.GetFileName(path), Path = path, Type = type });
        }

        /// <inheritdoc />
        public IEnumerable<HistoryEvent>? GetHistory(string path, int limit)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public IEnumerable<HistoryEvent>? GetHistory(string path, HistoryType type, int limit)
        {
            throw new NotImplementedException();
        }

        #endregion

        #region Manifests

        /// <inheritdoc />
        public async Task<int> AddManifestAsync(SystemFile file)
        {
            using IDbConnection connection = CreateConnection();
            string sql = @"INSERT OR REPLACE INTO manifests (name, path, lastwrite) VALUES (@Name, @Path, @LastWrite);";
            return await connection.ExecuteAsync(sql, new { file.Name, Path = file.LocalPath, LastWrite = file.LastWrite.TotalMilliseconds });
        }

        #endregion
    }
}