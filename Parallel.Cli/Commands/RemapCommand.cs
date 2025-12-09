// Copyright 2025 Kyle Ebbinga

using System.CommandLine;

namespace Parallel.Cli.Commands
{
    public class RemapCommand : Command
    {
        public RemapCommand() : base("remap", "Remaps paths in the vault.")
        {
        }
    }
}