// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using System.Diagnostics;
using System.IO.Compression;
using Parallel.Cli.Utils;
using Parallel.Core.Database;
using Parallel.Core.IO;
using Parallel.Core.Models;
using Parallel.Core.Security;
using Parallel.Core.Settings;
using Parallel.Core.Utils;

namespace Parallel.Cli.Commands
{
    public class EncryptCommand : Command
    {
        private readonly Argument<string> _sourceArg = new("path", "The source path to encrypt.");
        private readonly Option<string> _configOpt = new(["--config", "-c"], "The vault configuration to use.");

        private IDatabase? _database;
        private Stopwatch _sw = new Stopwatch();
        private List<Task> _tasks = new List<Task>();
        private int _totalTasks = 0;

        public EncryptCommand() : base("encrypt", "Encrypts a file or directory.")
        {
            this.AddArgument(_sourceArg);
            this.SetHandler(async (path, config) =>
            {
                _sw = Stopwatch.StartNew();
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

        private async Task EncryptDirectoryAsync(string path, string masterKey)
        {
            CommandLine.WriteLine($"Scanning for files in {path}...", ConsoleColor.DarkGray);
            string[] files = Directory.EnumerateFiles(path, $"*", SearchOption.AllDirectories).ToArray();
            if (files.Length == 0)
            {
                CommandLine.WriteLine("No files found to encrypt!", ConsoleColor.Yellow);
                return;
            }

            CommandLine.WriteLine($"Encrypting {files.Length.ToString("N0")} files...", ConsoleColor.DarkGray);
            _totalTasks = files.Length;
            _tasks = files.Select(file => Task.Run(async () =>
            {
                await EncryptFileAsync(file, masterKey);
                CommandLine.ProgressBar(_tasks.Count(t => t.IsCompleted), _totalTasks, _sw.Elapsed, ConsoleColor.DarkGray);
            })).ToList();

            await Task.WhenAll(_tasks);
            CommandLine.WriteLine($"Successfully encrypted {files.Length.ToString("N0")} files in {_sw.Elapsed}.", ConsoleColor.Green);
        }

        private async Task EncryptFileAsync(string path, string masterKey)
        {
            SystemFile systemFile = await _database?.GetFileAsync(path)! ?? new SystemFile(path);
            if (File.Exists(systemFile.LocalPath) && !systemFile.Encrypted)
            {
                string tempFile = Path.Combine(PathBuilder.TempDirectory, Path.GetFileName(systemFile.LocalPath)) + ".tmp";
                await using (FileStream openFile = new FileStream(systemFile.LocalPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                await using (FileStream createFile = new FileStream(tempFile, FileMode.OpenOrCreate))
                {
                    systemFile.Salt = HashGenerator.RandomBytes(16);
                    systemFile.IV = HashGenerator.RandomBytes(16);
                    systemFile.Encrypted = true;

                    Encryption.EncryptStream(openFile, createFile, masterKey, systemFile.LastWrite, systemFile.Salt, systemFile.IV);
                    if (!await _database?.AddFileAsync(systemFile)!)
                    {
                        CommandLine.WriteLine($"Failed to encrypt file: {systemFile.LocalPath}", ConsoleColor.Red);
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