// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using System.Reflection;
using Microsoft.Win32;
using Parallel.Cli.Utils;

namespace Parallel.Cli.Commands
{
    public class InstallCommand : Command
    {
        private readonly string _exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName ?? AppContext.BaseDirectory;

        public InstallCommand() : base("install", "Installs Parallel to the system.")
        {
            this.SetHandler(() =>
            {
                InstallWindows();
                InstallMac();
                InstallLinux();
            });
        }

        #region Windows

        private void InstallWindows()
        {
            if (!OperatingSystem.IsWindows()) return;
            CommandLine.WriteLine($"Installing for Windows...", ConsoleColor.DarkGray);

            (string Path, string Label, string Arguments)[] registryEntries =
            [
                (@"*\shell\ParallelPush", "Push this file", "push -p \"%1\""),
                (@"*\shell\ParallelClean", "Clean this file", "push -p \"%1\""),
                (@"*\shell\ParallelPrune", "Prune this file", "push -p \"%1\""),

                (@"Directory\shell\ParallelPush", "Push this folder", "push -p \"%1\""),
                (@"Directory\shell\ParallelClean", "Clean this folder", "push -p \"%1\""),
                (@"Directory\shell\ParallelPrune", "Prune this folder", "push -p \"%1\""),

                (@"Directory\Background\shell\ParallelPull", "Pull files here", "pull -p \"%V\""),
                (@"Directory\Background\shell\ParallelClean", "Clean files here", "push -p \"%V\""),
                (@"Directory\Background\shell\ParallelPrune", "Prune files here", "push -p \"%V\""),
            ];

            try
            {
                foreach ((string Path, string Label, string Arguments) e in registryEntries)
                {
                    using RegistryKey key = Registry.ClassesRoot.CreateSubKey(e.Path, true);
                    key.SetValue("", e.Label);
                    key.SetValue("Icon", _exePath);

                    using RegistryKey commandKey = key.CreateSubKey("command");
                    commandKey.SetValue("", $"\"{_exePath}\" {e.Arguments}");
                }
            }
            catch (Exception ex)
            {
                CommandLine.WriteLine($"Unable to add registry keys: {ex.Message}", ConsoleColor.Yellow);
            }
        }

        #endregion

        private void InstallMac()
        {
            if (!OperatingSystem.IsMacOS()) return;
            CommandLine.WriteLine($"Installing for Mac...", ConsoleColor.DarkGray);
        }

        private void InstallLinux()
        {
            if (!OperatingSystem.IsLinux()) return;
            CommandLine.WriteLine($"Installing for Linux...", ConsoleColor.DarkGray);
        }
    }
}