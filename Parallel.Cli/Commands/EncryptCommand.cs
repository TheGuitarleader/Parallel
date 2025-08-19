// Copyright 2025 Kyle Ebbinga

using System.CommandLine;

namespace Parallel.Cli.Commands
{
    public class EncryptCommand : Command
    {
        private readonly Argument<string> sourceArg = new("path", "The source path to encrypt.");

        public EncryptCommand() : base("encrypt", "Encrypts a file or directory.")
        {

        }
    }
}