// Copyright 2025 Kyle Ebbinga

using Newtonsoft.Json.Linq;
using Parallel.Core.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Parallel.Core.IO.Syncing;

namespace Parallel.Core.IO.Backup
{
    /// <summary>
    /// Represents the way manage <see cref="ISyncManager"/>s.
    /// </summary>
    public static class SyncManager
    {
        /// <summary>
        /// Creates a new instance of an <see cref="ISyncManager"/>.
        /// </summary>
        /// <param name="vault"></param>
        /// <returns></returns>
        public static ISyncManager CreateNew(VaultConfig vault)
        {
            return new FileSyncManager(vault);
        }
    }
}