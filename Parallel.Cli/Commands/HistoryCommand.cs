// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using Parallel.Cli.Utils;
using Parallel.Core.Database;
using Parallel.Core.IO.Syncing;
using Parallel.Core.Models;
using Parallel.Core.Settings;
using Parallel.Core.Utils;

namespace Parallel.Cli.Commands
{
    public class HistoryCommand : Command
    {
        private const int _limit = 25;

        private readonly Command _archiveCmd = new("archive", "Shows the history related to file deletions.");
        private readonly Command _cleanCmd = new("clean", "Shows the history related to file cleaning.");
        private readonly Command _cloneCmd = new("clone", "Shows the history related to file cloning.");
        private readonly Command _pullCmd = new("pull", "Shows the history related to file pulling.");
        private readonly Command _pushCmd = new("push", "Shows the history related to file pushing.");
        private readonly Command _pruneCmd = new("prune", "Shows the history related to file pruning.");

        private readonly Option<string> _sourceOpt = new(["--path", "-p"], "The source path to push.");
        private readonly Option<string> _configOpt = new(["--config", "-c"], "The vault configuration to use.");
        private readonly Option<int> _limitOpt = new(["--limit", "-l"], "The number of entries to show.");

        public HistoryCommand() : base("history", "Displays the history of a vault.")
        {
            this.AddCommand(_archiveCmd);
            this.AddCommand(_cleanCmd);
            this.AddCommand(_cloneCmd);
            this.AddCommand(_pullCmd);
            this.AddCommand(_pushCmd);
            this.AddCommand(_pruneCmd);
            this.AddOption(_sourceOpt);
            this.AddOption(_configOpt);
            this.AddOption(_limitOpt);
            this.SetHandler(async (path, config, limit) =>
            {
                await DisplayHistoryAsync(path, config, limit);
            }, _sourceOpt, _configOpt, _limitOpt);

            _archiveCmd.AddOption(_sourceOpt);
            _archiveCmd.AddOption(_configOpt);
            _archiveCmd.AddOption(_limitOpt);
            _archiveCmd.SetHandler(async (path, config, limit) =>
            {
                await DisplayHistoryAsync(HistoryType.Archived, path, config, limit);
            }, _sourceOpt, _configOpt, _limitOpt);

            _cleanCmd.AddOption(_sourceOpt);
            _cleanCmd.AddOption(_configOpt);
            _cleanCmd.AddOption(_limitOpt);
            _cleanCmd.SetHandler(async (path, config, limit) =>
            {
                await DisplayHistoryAsync(HistoryType.Cleaned, path, config, limit);
            }, _sourceOpt, _configOpt, _limitOpt);

            _cloneCmd.AddOption(_sourceOpt);
            _cloneCmd.AddOption(_configOpt);
            _cloneCmd.AddOption(_limitOpt);
            _cloneCmd.SetHandler(async (path, config, limit) =>
            {
                await DisplayHistoryAsync(HistoryType.Cloned, path, config, limit);
            }, _sourceOpt, _configOpt, _limitOpt);

            _pullCmd.AddOption(_sourceOpt);
            _pullCmd.AddOption(_configOpt);
            _pullCmd.AddOption(_limitOpt);
            _pullCmd.SetHandler(async (path, config, limit) =>
            {
                await DisplayHistoryAsync(HistoryType.Pulled, path, config, limit);
            }, _sourceOpt, _configOpt, _limitOpt);

            _pushCmd.AddOption(_sourceOpt);
            _pushCmd.AddOption(_configOpt);
            _pushCmd.AddOption(_limitOpt);
            _pushCmd.SetHandler(async (path, config, limit) =>
            {
                await DisplayHistoryAsync(HistoryType.Pushed, path, config, limit);
            }, _sourceOpt, _configOpt, _limitOpt);

            _pruneCmd.AddOption(_sourceOpt);
            _pruneCmd.AddOption(_configOpt);
            _pruneCmd.AddOption(_limitOpt);
            _pruneCmd.SetHandler(async (path, config, limit) =>
            {
                await DisplayHistoryAsync(HistoryType.Pruned, path, config, limit);
            }, _sourceOpt, _configOpt, _limitOpt);
        }

        private async Task DisplayHistoryAsync(string path, string config, int limit)
        {
            if (limit == 0) limit = _limit;
            LocalVaultConfig? vault = ParallelConfig.Load().Vaults.FirstOrDefault(v => v.Enabled);
            if (!string.IsNullOrEmpty(config)) vault = ParallelConfig.GetVault(config);
            if (vault == null)
            {
                CommandLine.WriteLine($"No vault was found!", ConsoleColor.Yellow);
                return;
            }

            ISyncManager? syncManager = SyncManager.CreateNew(vault);
            if (syncManager == null || !await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, $"Failed to connect to vault!", ConsoleColor.Red);
                return;
            }

            IEnumerable<HistoryEvent> historyList = await (syncManager.Database?.GetHistoryAsync(path, limit) ?? Task.FromResult<IEnumerable<HistoryEvent>>([]));
            if (!historyList.Any()) CommandLine.WriteLine("No history was found!", ConsoleColor.Yellow);
            foreach (HistoryEvent historyEvent in historyList)
            {
                CommandLine.WriteLine($"[{Formatter.FromDateTime(historyEvent.CreatedAt.ToLocalTime())}] {historyEvent.Type + ":",-9} {historyEvent.Fullname}", ConsoleColor.White);
            }
        }

        private async Task DisplayHistoryAsync(HistoryType type, string path, string config, int limit)
        {
            if (limit == 0) limit = _limit;
            LocalVaultConfig? vault = ParallelConfig.Load().Vaults.FirstOrDefault(v => v.Enabled);
            if (!string.IsNullOrEmpty(config)) vault = ParallelConfig.GetVault(config);
            if (vault == null)
            {
                CommandLine.WriteLine($"No vault was found!", ConsoleColor.Yellow);
                return;
            }

            ISyncManager? syncManager = SyncManager.CreateNew(vault);
            if (syncManager == null || !await syncManager.ConnectAsync())
            {
                CommandLine.WriteLine(vault, $"Failed to connect to vault!", ConsoleColor.Red);
                return;
            }

            IEnumerable<HistoryEvent> historyList = await (syncManager.Database?.GetHistoryAsync(path, type, limit) ?? Task.FromResult<IEnumerable<HistoryEvent>>([]));
            if (!historyList.Any()) CommandLine.WriteLine("No history was found!", ConsoleColor.Yellow);
            foreach (HistoryEvent historyEvent in historyList)
            {
                CommandLine.WriteLine($"[{Formatter.FromDateTime(historyEvent.CreatedAt.ToLocalTime())}] {historyEvent.Type}: {historyEvent.Fullname}", ConsoleColor.White);
            }
        }
    }
}