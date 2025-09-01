// Copyright 2025 Entex Interactive, LLC

using System.CommandLine;
using System.Data;
using Parallel.Cli.Utils;
using Parallel.Core.Database;
using Parallel.Core.IO;
using Parallel.Core.IO.Backup;
using Parallel.Core.Models;
using Parallel.Core.Settings;
using Parallel.Core.Utils;
using Formatter = Parallel.Cli.Utils.Formatter;

namespace Parallel.Cli.Commands
{
    public class HistoryCommand : Command
    {
        private const int Limit = 25;

        private Command _pullCmd = new("pull", "Shows the history related to pulling files from vaults.");
        private Command _pushCmd = new("push", "Shows the history related to pushing files from vaults.");
        private Command _deleteCmd = new("archive", "Shows the history related to file deletions.");
        private Command _cleanCmd = new("cleaned", "Shows the history related to file cleaning.");
        private Command _cloneCmd = new("cloned", "Shows the history related to file cloning.");
        private Command _pruneCmd = new("pruned", "Shows the history related to file pruning.");

        private Option<string> _sourceOpt = new(["--path", "-p"], "The source path.");
        private Option<string> _vaultOpt = new(["--vault", "-v"], "The vault to use.");
        private Option<int> _limitOpt = new(["--limit", "-l"], "The number of entries to show.");

        public HistoryCommand() : base("history", "Shows the history of files related to the archive.")
        {
            this.AddOption(_sourceOpt);
            this.AddOption(_vaultOpt);
            this.AddOption(_limitOpt);
            this.AddCommand(_pullCmd);
            this.AddCommand(_pushCmd);
            this.AddCommand(_deleteCmd);
            this.AddCommand(_cleanCmd);
            this.AddCommand(_cloneCmd);
            this.AddCommand(_pruneCmd);
            this.SetHandler((path, config, limit) =>
            {
                CommandLine.WriteLine($"Retrieving backup information...", ConsoleColor.DarkGray);
                IDatabase? db = DatabaseConnection.CreateNew(VaultConfig.Load(Program.Settings, config));

                if (limit == 0) limit = Limit;
                DisplayHistories(db?.GetHistory(path, limit).ToArray());
            }, _sourceOpt, _vaultOpt, _limitOpt);

            _pushCmd.AddOption(_sourceOpt);
            _pushCmd.AddOption(_vaultOpt);
            _pushCmd.AddOption(_limitOpt);
            _pushCmd.SetHandler((path, config, limit) =>
            {
                CommandLine.WriteLine($"Retrieving backup information...", ConsoleColor.DarkGray);
                IDatabase? db = DatabaseConnection.CreateNew(VaultConfig.Load(Program.Settings, config));

                if (limit == 0) limit = Limit;
                DisplayHistories(db?.GetHistory(path, HistoryType.Pushed, limit).ToArray());
            }, _sourceOpt, _vaultOpt, _limitOpt);

            _deleteCmd.AddOption(_sourceOpt);
            _deleteCmd.AddOption(_vaultOpt);
            _deleteCmd.AddOption(_limitOpt);
            _deleteCmd.SetHandler((path, config, limit) =>
            {
                CommandLine.WriteLine($"Retrieving archive information...", ConsoleColor.DarkGray);
                IDatabase? db = DatabaseConnection.CreateNew(VaultConfig.Load(Program.Settings, config));

                if (limit == 0) limit = Limit;
                DisplayHistories(db?.GetHistory(path, HistoryType.Archived, limit).ToArray());
            }, _sourceOpt, _vaultOpt, _limitOpt);

            _cleanCmd.AddOption(_sourceOpt);
            _cleanCmd.AddOption(_vaultOpt);
            _cleanCmd.AddOption(_limitOpt);
            _cleanCmd.SetHandler((path, config, limit) =>
            {
                CommandLine.WriteLine($"Retrieving clean information...", ConsoleColor.DarkGray);
                IDatabase? db = DatabaseConnection.CreateNew(VaultConfig.Load(Program.Settings, config));

                if (limit == 0) limit = Limit;
                DisplayHistories(db?.GetHistory(path, HistoryType.Cleaned, limit).ToArray());
            }, _sourceOpt, _vaultOpt, _limitOpt);

            _cloneCmd.AddOption(_sourceOpt);
            _cloneCmd.AddOption(_vaultOpt);
            _cloneCmd.AddOption(_limitOpt);
            _cloneCmd.SetHandler((path, config, limit) =>
            {
                CommandLine.WriteLine($"Retrieving clone information...", ConsoleColor.DarkGray);
                IDatabase db = DatabaseConnection.CreateNew(VaultConfig.Load(Program.Settings, config));

                if (limit == 0) limit = Limit;
                DisplayHistories(db?.GetHistory(path, HistoryType.Cloned, limit).ToArray());
            }, _sourceOpt, _vaultOpt, _limitOpt);

            _pruneCmd.AddOption(_sourceOpt);
            _pruneCmd.AddOption(_vaultOpt);
            _pruneCmd.AddOption(_limitOpt);
            _pruneCmd.SetHandler((path, config, limit) =>
            {
                CommandLine.WriteLine($"Retrieving prune information...", ConsoleColor.DarkGray);
                IDatabase db = DatabaseConnection.CreateNew(VaultConfig.Load(Program.Settings, config));

                if (limit == 0) limit = Limit;
                DisplayHistories(db?.GetHistory(path, HistoryType.Pruned, limit).ToArray());
            }, _sourceOpt, _vaultOpt, _limitOpt);

            _pullCmd.AddOption(_sourceOpt);
            _pullCmd.AddOption(_vaultOpt);
            _pullCmd.AddOption(_limitOpt);
            _pullCmd.SetHandler((path, config, limit) =>
            {
                CommandLine.WriteLine($"Retrieving restore information...", ConsoleColor.DarkGray);
                IDatabase db = DatabaseConnection.CreateNew(VaultConfig.Load(Program.Settings, config));

                if (limit == 0) limit = Limit;
                DisplayHistories(db?.GetHistory(path, HistoryType.Pulled, limit).ToArray());
            }, _sourceOpt, _vaultOpt, _limitOpt);
        }

        private void DisplayHistories(HistoryEvent[]? histories)
        {
            if (histories?.Length == 0)
            {
                CommandLine.WriteLine("No backup history found!", ConsoleColor.Yellow);
                return;
            }

            foreach (HistoryEvent history in histories.ToArray())
            {
                string typeStr = (history.Type + ":").PadRight(9);
                CommandLine.WriteLine($"[{Formatter.FromDateTime(history.CreatedAt.ToLocalTime())}] <{history.Vault}> {typeStr} {history.Fullname}", ConsoleColor.White);
            }
        }
    }
}