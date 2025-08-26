// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using System.Diagnostics;
using Parallel.Cli.Utils;
using Parallel.Core.Database;
using Parallel.Core.IO;
using Parallel.Core.Models;
using Parallel.Core.Settings;
using Parallel.Core.Utils;

namespace Parallel.Cli.Commands
{
    public class DecryptCommand : Command
    {
        private readonly Argument<string> _sourceArg = new("path", "The source path of files to zip.");
        private readonly Option<string> _configOpt = new(["--config", "-c"], "The vault configuration to use.");

        private IDatabase? _database;
        private Stopwatch _sw = new Stopwatch();
        private List<Task> _tasks = new List<Task>();
        private int _totalTasks = 0;

        public DecryptCommand() : base("decrypt", "Decrypts a file or directory.")
        {
            this.AddArgument(_sourceArg);
            this.SetHandler(async (path, config) =>
            {
                VaultConfig? vault = VaultConfig.Load(Program.Settings, config);
                if (vault == null)
                {
                    CommandLine.WriteLine("No active vault was found!", ConsoleColor.Yellow);
                    return;
                }

                _database = DatabaseConnection.CreateNew(vault);
                string masterKey = vault.FileSystem.EncryptionKey ?? throw new ArgumentException("No encryption key provided!");
                if (PathBuilder.IsDirectory(path))
                {
                    await DecryptDirectoryAsync(path, masterKey);
                }
                else if (PathBuilder.IsFile(path))
                {
                    CommandLine.WriteLine($"Decrypting {path}...", ConsoleColor.DarkGray);
                    await DecryptFileAsync(path, masterKey);

                    CommandLine.WriteLine($"Successfully decrypted file: {path}", ConsoleColor.Green);
                }
                else
                {
                    CommandLine.WriteLine("The specified path is invalid.", ConsoleColor.Red);
                }
            }, _sourceArg, _configOpt);
        }

        private async Task DecryptDirectoryAsync(string path, string masterKey)
        {
            CommandLine.WriteLine($"Scanning for files in {path}...", ConsoleColor.DarkGray);
            string[] files = Directory.EnumerateFiles(path, $"*", SearchOption.AllDirectories).ToArray();
            if (files.Length == 0)
            {
                CommandLine.WriteLine("No files found to decrypt!", ConsoleColor.Yellow);
                return;
            }

            CommandLine.WriteLine($"Decrypting {files.Length.ToString("N0")} files...", ConsoleColor.DarkGray);
            _totalTasks = files.Length;
            _tasks = files.Select(file => Task.Run(async () =>
            {
                await DecryptFileAsync(file, masterKey);
                CommandLine.ProgressBar(_tasks.Count(t => t.IsCompleted), _totalTasks, _sw.Elapsed, ConsoleColor.DarkGray);
            })).ToList();

            await Task.WhenAll(_tasks);
            CommandLine.WriteLine($"Successfully decrypted {files.Length.ToString("N0")} files in {_sw.Elapsed}.", ConsoleColor.Green);
        }

        private async Task DecryptFileAsync(string path, string masterKey)
        {
            SystemFile systemFile = await _database?.GetFileAsync(path)! ?? new SystemFile(path);
            if (File.Exists(systemFile.LocalPath) && systemFile.Encrypted)
            {
                string tempFile = Path.Combine(PathBuilder.TempDirectory, Path.GetFileName(systemFile.LocalPath)) + ".tmp";
                await using (FileStream openFile = new FileStream(systemFile.LocalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                await using (FileStream createFile = new FileStream(tempFile, FileMode.OpenOrCreate))
                {
                    systemFile.Encrypted = false;

                    Encryption.DecryptStream(openFile, createFile, masterKey, systemFile.LastWrite, systemFile.Salt, systemFile.IV);
                    if (!await _database?.AddFileAsync(systemFile)!)
                    {
                        CommandLine.WriteLine($"Failed to decrypt file: {systemFile.LocalPath}", ConsoleColor.Red);
                        if(File.Exists(tempFile)) File.Delete(tempFile);
                        return;
                    }
                }

                File.Copy(tempFile, systemFile.LocalPath, true);
                if(File.Exists(tempFile)) File.Delete(tempFile);
            }
        }
    }
}