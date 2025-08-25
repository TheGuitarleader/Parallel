// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using System.IO.Compression;
using Parallel.Cli.Utils;
using Parallel.Core.Database;
using Parallel.Core.IO;
using Parallel.Core.Models;
using Parallel.Core.Settings;
using Parallel.Core.Utils;

namespace Parallel.Cli.Commands
{
    public class EncryptCommand : Command
    {
        private readonly Argument<string> _sourceArg = new("path", "The source path to encrypt.");
        private readonly Option<string> _configOpt = new(["--config", "-c"], "The profile configuration to use.");

        private IDatabase? _database;

        public EncryptCommand() : base("encrypt", "Encrypts a file or directory.")
        {
            this.AddArgument(_sourceArg);
            this.SetHandler(async (path, config) =>
            {
                ProfileConfig? profile = ProfileConfig.Load(Program.Settings, config);
                if (profile == null)
                {
                    CommandLine.WriteLine("No active profile was found!", ConsoleColor.Yellow);
                    return;
                }

                _database = DatabaseConnection.CreateNew(profile);
                string masterKey = profile.FileSystem.EncryptionKey ?? throw new ArgumentException("No encryption key provided!");
                if (PathBuilder.IsDirectory(path))
                {
                    await EncryptDirectoryAsync(path, masterKey);
                }
                else if (PathBuilder.IsFile(path))
                {
                    CommandLine.WriteLine($"Encrypting {path}...", ConsoleColor.DarkGray);
                    await EncryptFileAsync(path, masterKey);

                    CommandLine.WriteLine($"Successfully encrypted file: {path}", ConsoleColor.Green);
                }
                else
                {
                    CommandLine.WriteLine("The specified path is invalid.", ConsoleColor.Red);
                }

            }, _sourceArg, _configOpt);
        }

        private Task EncryptDirectoryAsync(string path, string masterKey)
        {
            CommandLine.WriteLine($"Scanning for files in {path}...", ConsoleColor.DarkGray);
            return Task.CompletedTask;
        }

        private async Task EncryptFileAsync(string path, string masterKey)
        {
            SystemFile systemFile = await _database?.GetFileAsync(path)! ?? new SystemFile(path);
            if (File.Exists(systemFile.LocalPath) && !systemFile.Encrypted)
            {
                string tempFile = Path.Combine(PathBuilder.TempDirectory, Path.GetFileName(systemFile.LocalPath)) + ".tmp";
                CommandLine.WriteLine($"Writing to {tempFile}", ConsoleColor.DarkGray);

                await using (FileStream openFile = new FileStream(systemFile.LocalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                await using (FileStream createFile = new FileStream(tempFile, FileMode.OpenOrCreate))
                {
                    SystemFile result = Encryption.EncryptStream(openFile, createFile, systemFile, masterKey);
                    if (!await _database?.AddFileAsync(result)!)
                    {
                        CommandLine.WriteLine($"Failed to decrypt file: {systemFile.LocalPath}", ConsoleColor.Red);
                        if(File.Exists(tempFile)) File.Delete(tempFile);
                    }
                }

                File.Copy(tempFile, systemFile.LocalPath, true);
                //if(File.Exists(tempFile)) File.Delete(tempFile);
            }

            //CommandLine.ProgressBar(_tasks.Count(t => t.IsCompleted), _totalTasks, _sw.Elapsed, ConsoleColor.DarkGray);
        }
    }
}