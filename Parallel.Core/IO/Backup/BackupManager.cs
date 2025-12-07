// Copyright 2025 Kyle Ebbinga

using Newtonsoft.Json.Linq;
using Parallel.Core.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parallel.Core.IO.Backup
{
    /// <summary>
    /// Represents the way manage <see cref="IBackupManager"/>s.
    /// </summary>
    public static class BackupManager
    {
        /// <summary>
        /// Creates a new instance of an <see cref="IBackupManager"/>.
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        public static IBackupManager CreateNew(ProfileConfig profile)
        {
            return new FileBackupManager(profile);
        }
    }
}