// Copyright 2026 Kyle Ebbinga

using System.Data;
using Microsoft.Data.Sqlite;
using Parallel.Core.Models;
using Parallel.Core.Utils;

namespace Parallel.Core.Database.Contexts
{
    /// <inheritdoc />
    public class SqliteContext : IDatabase
    {
        private readonly SemaphoreContext _semaphore;

        /// <summary>
        /// Initializes a new instance of the <see cref="SqliteContext"/> class.
        /// </summary>
        /// <param name="filePath"></param>
        public SqliteContext(string filePath)
        {
            _semaphore = new SemaphoreContext($"Data Source={filePath};Cache=Shared;Mode=ReadWriteCreate;Pooling=false;");
        }

        /// <inheritdoc />
        public async Task InitializeAsync()
        {
            Log.Information("Creating index database...");
            await _semaphore.ExecuteAsync("CREATE TABLE IF NOT EXISTS `objects` (`name` TEXT NOT NULL, `localpath` TEXT NOT NULL, `remotepath` TEXT NOT NULL, `lastwrite` LONG INTEGER NOT NULL, `lastupdate` LONG INTEGER NOT NULL, `localsize` LONG INTEGER NOT NULL, `remotesize` LONG INTEGER NOT NULL, `type` TEXT NOT NULL DEFAULT Other CHECK(`type` IN ('Document', 'Photo', 'Music', 'Video', 'Other')), `hidden` INTEGER NOT NULL DEFAULT 0, `readonly` INTEGER NOT NULL DEFAULT 0, `deleted` INTEGER NOT NULL DEFAULT 0, `checksum` TEXT, UNIQUE (localpath, checksum));");
            await _semaphore.ExecuteAsync("CREATE TABLE IF NOT EXISTS `history` (`timestamp` LONG INTEGER NOT NULL, `path` TEXT NOT NULL, `checksum` TEXT NOT NULL, `type` INTEGER NOT NULL, PRIMARY KEY(`timestamp`));");
            await _semaphore.ExecuteAsync("CREATE INDEX idx_objects_path_update ON objects(localpath, lastupdate DESC, deleted);");
        }

        #region Objects

        /// <exception cref="ArgumentNullException"></exception>
        /// <inheritdoc />
        public async Task<bool> AddFileAsync(SystemFile file)
        {
            if (string.IsNullOrEmpty(file.LocalPath)) throw new ArgumentNullException(nameof(file.LocalPath));
            if (!file.TryGenerateCheckSum()) throw new ArgumentNullException(nameof(file.CheckSum));

            string sql = @"INSERT OR REPLACE INTO objects (name, localpath, remotepath, lastwrite, lastupdate, LocalSize, RemoteSize, type, hidden, readonly, deleted, checksum) VALUES (@Name, @LocalPath, @RemotePath, @LastWrite, @LastUpdate, @LocalSize, @RemoteSize, @Type, @Hidden, @ReadOnly, @Deleted, @CheckSum);";
            return await _semaphore.ExecuteAsync(sql, new { file.Name, file.LocalPath, file.RemotePath, LastWrite = file.LastWrite.TotalMilliseconds, LastUpdate = UnixTime.Now.TotalMilliseconds, file.LocalSize, file.RemoteSize, Type = file.Type.ToString(), file.Hidden, file.ReadOnly, file.Deleted, file.CheckSum }) > 0;
        }

        /// <inheritdoc />
        public async Task RemoveFileAsync(SystemFile file)
        {
            string sql = $"DELETE FROM objects WHERE localpath = @LocalPath AND checksum = @Checksum;";
            await _semaphore.ExecuteAsync(sql, new { file.LocalPath, file.CheckSum });
        }

        /// <inheritdoc />
        public async Task<long> GetLocalSizeAsync()
        {
            string sql = "SELECT COALESCE(SUM(f.localsize), 0) FROM objects f JOIN (SELECT localpath, MAX(lastupdate) AS max_lastupdate FROM objects WHERE deleted = 0 GROUP BY localpath) latest ON f.localpath = latest.localpath AND f.lastupdate = latest.max_lastupdate;";
            return await _semaphore.QuerySingleAsync<long>(sql);
        }

        /// <inheritdoc />
        public async Task<long> GetRemoteSizeAsync()
        {
            string sql = "SELECT COALESCE(SUM(f.remotesize), 0) FROM objects f JOIN (SELECT checksum, MAX(lastupdate) AS max_lastupdate FROM objects GROUP BY checksum) latest ON f.checksum = latest.checksum AND f.lastupdate = latest.max_lastupdate;";
            return await _semaphore.QuerySingleAsync<long>(sql);
        }

        /// <inheritdoc />
        public async Task<long> GetTotalSizeAsync()
        {
            string sql = $"SELECT COALESCE(SUM(remotesize), 0) FROM objects;";
            return await _semaphore.QuerySingleAsync<long>(sql);
        }

        /// <inheritdoc />
        public async Task<long> GetTotalFilesAsync()
        {
            string sql = $"SELECT COUNT(*) FROM objects;";
            return await _semaphore.QuerySingleAsync<long>(sql);
        }

        /// <inheritdoc />
        public async Task<long> GetTotalFilesAsync(bool deleted)
        {
            string sql = $"SELECT COUNT(DISTINCT localpath) FROM objects WHERE deleted = @deleted;";
            return await _semaphore.QuerySingleAsync<long>(sql, new { deleted });
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<SystemFile>> GetLatestFilesAsync(string path, DateTime timestamp)
        {
            string sql = "SELECT * FROM (SELECT * FROM objects WHERE localpath LIKE @Path AND lastupdate <= @Time ORDER BY lastupdate DESC) GROUP BY localpath;";
            return await _semaphore.QueryAsync<SystemFile>(sql, new { Path = $"%{path}%", Time = new UnixTime(timestamp).TotalMilliseconds });
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<SystemFile>> GetLatestFilesAsync(string path, DateTime timestamp, bool deleted)
        {
            string sql = "SELECT * FROM (SELECT * FROM objects WHERE localpath LIKE @Path AND lastupdate <= @Time AND deleted = @deleted ORDER BY lastupdate DESC) GROUP BY localpath;";
            return await _semaphore.QueryAsync<SystemFile>(sql, new { Path = $"%{path}%", Time = new UnixTime(timestamp).TotalMilliseconds, deleted });
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<SystemFile>> GetRevisedFilesAsync(string path)
        {
            string sql = "SELECT * FROM objects WHERE localpath LIKE @Path OR remotepath LIKE @Path ORDER BY lastupdate DESC";
            return await _semaphore.QueryAsync<SystemFile>(sql, new { Path = $"%{path}%" });
        }

        /// <inheritdoc />
        public async Task<SystemFile?> GetFileAsync(string path)
        {
            string sql = $"SELECT (name, localpath, remotepath, lastwrite, lastupdate, localsize, remotesize, type, hidden, readonly, deleted, checksum) FROM objects WHERE localpath LIKE \"%@path%\" OR remotepath LIKE \"%@path%\" ORDER BY lastupdate DESC";
            return await _semaphore.QuerySingleAsync<SystemFile>(sql, new { path });
        }

        #endregion

        #region History

        /// <inheritdoc />
        public async Task<bool> AddHistoryAsync(HistoryType type, SystemFile file)
        {
            if (string.IsNullOrEmpty(file.LocalPath)) throw new ArgumentNullException(nameof(file.LocalPath));
            if (string.IsNullOrEmpty(file.CheckSum)) throw new ArgumentNullException(nameof(file.CheckSum));

            string sql = @"INSERT OR REPLACE INTO history (timestamp, path, checksum, type) VALUES(@Timestamp, @Path, @CheckSum, @Type);";
            return await _semaphore.ExecuteAsync(sql, new { Timestamp = file.LastUpdate.TotalMilliseconds, Path = file.LocalPath, file.CheckSum, Type = type }) > 0;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<HistoryEvent>> GetHistoryAsync(string path, int limit)
        {
            string sql = "SELECT * FROM history WHERE path LIKE @Path ORDER BY timestamp DESC LIMIT @limit;";
            return await _semaphore.QueryAsync<HistoryEvent>(sql, new { Path = $"%{path}%", limit });
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<HistoryEvent>> GetHistoryAsync(string path, HistoryType type, int limit)
        {
            string sql = "SELECT * FROM history WHERE path LIKE @Path AND type = @type ORDER BY timestamp DESC LIMIT @limit;";
            return await _semaphore.QueryAsync<HistoryEvent>(sql, new { Path = $"%{path}%", type, limit });
        }

        #endregion
    }
}