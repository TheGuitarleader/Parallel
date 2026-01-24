// Copyright 2026 Kyle Ebbinga

using System.Data;
using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Parallel.Core.Database;
using Parallel.Core.Database.Contexts;
using Parallel.Core.IO;
using Parallel.Core.Models;
using Parallel.Core.Settings;
using Parallel.Core.Utils;
using Serilog;

namespace Parallel.Service.Controllers
{
    [ApiController]
    [Route("vaults")]
    public class VaultController : Controller
    {
        [HttpGet]
        public Task<ActionResult> GetVaultsAsync()
        {
            HashSet<LocalVaultConfig> vaults = ParallelConfig.Load().Vaults;
            return Task.FromResult<ActionResult>(Json(vaults));
        }

        [HttpGet, Route("{id}")]
        public Task<ActionResult> GetVaultAsync([FromRoute] string id)
        {
            LocalVaultConfig? vault = ParallelConfig.Load().Vaults.FirstOrDefault(x => x.Id == id);
            return Task.FromResult<ActionResult>(Json(vault));
        }

        [HttpGet, Route("{id}/stats")]
        public async Task<ActionResult> GetVaultStatsAsync([FromRoute] string id)
        {
            string tempDbFile = Path.Combine(PathBuilder.TempDirectory, id + ".db");
            if (!System.IO.File.Exists(tempDbFile)) return BadRequest();

            SqliteContext db = new SqliteContext(tempDbFile);
            string lastSync = Formatter.FromDateTime(await (db?.GetLastSyncTimeAsync() ?? Task.FromResult(DateTime.MinValue)));
            long localSize = await (db?.GetLocalSizeAsync() ?? Task.FromResult(0L));
            long remoteSize = await (db?.GetRemoteSizeAsync() ?? Task.FromResult(0L));
            long totalSize = await (db?.GetTotalSizeAsync() ?? Task.FromResult(0L));
            long totalFiles = await (db?.GetTotalFilesAsync() ?? Task.FromResult(0L));
            long totalLocalFiles = await (db?.GetTotalFilesAsync(false) ?? Task.FromResult(0L));
            long totalDeletedFiles = await (db?.GetTotalFilesAsync(true) ?? Task.FromResult(0L));
            return Json(new { localSize, remoteSize, totalSize, totalFiles, totalLocalFiles, totalDeletedFiles });
        }

        [HttpGet, Route("{id}/directories")]
        public async Task<ActionResult> GetDirectoriesAsync([FromRoute] string id, string path)
        {
            string tempDbFile = Path.Combine(PathBuilder.TempDirectory, id + ".db");
            if (!System.IO.File.Exists(tempDbFile)) return BadRequest();

            SqliteContext db = new SqliteContext(tempDbFile);
            IReadOnlyList<string> files = await db.ListDirectoriesAsync(path);
            return Json(files);
        }

        [HttpGet, Route("{id}/files")]
        public async Task<ActionResult> GetFilesAsync([FromRoute] string id, string path)
        {
            string tempDbFile = Path.Combine(PathBuilder.TempDirectory, id + ".db");
            if (!System.IO.File.Exists(tempDbFile)) return BadRequest();

            SqliteContext db = new SqliteContext(tempDbFile);
            IReadOnlyList<SystemFile> files = await db.ListFilesAsync(path);
            return Json(files);
        }
    }
}