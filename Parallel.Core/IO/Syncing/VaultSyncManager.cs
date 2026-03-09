// Copyright 2026 Entex Interactive, LLC

using Parallel.Core.Diagnostics;
using Parallel.Core.Models;
using Parallel.Core.Settings;

namespace Parallel.Core.IO.Syncing
{
    public class VaultSyncManager : BaseSyncManager
    {
        public VaultSyncManager(LocalVaultConfig localVault) : base(localVault) { }

        public override Task<int> BackupFilesAsync(IReadOnlyList<SystemFile> files, IProgressReporter progress, bool overwrite)
        {
            throw new NotImplementedException();
        }

        public override Task<int> RestoreFilesAsync(IReadOnlyList<SystemFile> files, IProgressReporter progress)
        {
            throw new NotImplementedException();
        }

        public override Task<int> PruneFilesAsync(IReadOnlyList<SystemFile> files, IProgressReporter progress)
        {
            throw new NotImplementedException();
        }
    }
}