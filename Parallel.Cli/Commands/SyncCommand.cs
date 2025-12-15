// Copyright 2025 Kyle Ebbinga

using System.CommandLine;

namespace Parallel.Cli.Commands
{
    public class SyncCommand
    {
        private Command addCmd = new("add", "Adds a new directory to the sync list.");
        private Command listCmd = new("list", "Shows all directories in the sync list.");
        private Command removeCmd = new("remove", "Removes a directory from the sync list.");
    }
}