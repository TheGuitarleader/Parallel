// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using Parallel.Cli.Utils;
using Parallel.Core.IO;

namespace Parallel.Cli.Commands
{
    public class DecryptCommand : Command
    {
        private readonly Argument<string> _sourceArg = new("path", "The source path of files to zip.");

        public DecryptCommand() : base("decrypt", "Decrypts a file or directory.")
        {
            this.AddArgument(_sourceArg);
            this.SetHandler(async (path) =>
            {
                if (PathBuilder.IsDirectory(path))
                {
                    await DecryptDirectoryAsync(path);
                }
                else if (PathBuilder.IsFile(path))
                {
                    CommandLine.WriteLine($"Decrypting {path}...", ConsoleColor.DarkGray);
                    await DecryptFileAsync(path);
                }
                else
                {
                    CommandLine.WriteLine("The specified path is invalid.", ConsoleColor.Red);
                }
            }, _sourceArg);
        }

        private async Task DecryptDirectoryAsync(string path)
        {
            CommandLine.WriteLine($"Scanning for files in {path}...", ConsoleColor.DarkGray);
        }

        private async Task DecryptFileAsync(string path)
        {
        }
    }
}