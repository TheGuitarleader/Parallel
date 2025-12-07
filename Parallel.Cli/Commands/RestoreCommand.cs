// Copyright 2025 Kyle Ebbinga

using System.CommandLine;

namespace Parallel.Cli.Commands
{
    public class RestoreCommand : Command
    {
        private Command createCmd = new("create", "Creates a new recovery point current of the system state.");
        private Command listCmd = new("list", "Lists all available recovery points.");
        private Command restoreCmd = new("restore", "Restores the system state from a previous recovery point.");
        private Option<string> credsOpt = new(["--credentials", "-c"], "The file system credentials to use.");

        public RestoreCommand() : base("restore", "Creates or loads a system restore point.")
        {

        }
    }
}