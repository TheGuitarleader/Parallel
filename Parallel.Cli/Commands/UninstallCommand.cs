// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using Microsoft.Win32;
using Parallel.Cli.Utils;

namespace Parallel.Cli.Commands
{
    public class UnstallCommand : Command
    {
        public UnstallCommand() : base("uninstall", "Uninstalls Parallel to the system.")
        {
            this.SetHandler(() =>
            {
                UninstallWindows();
                UninstallMac();
                UninstallLinux();
            });
        }

        private void UninstallWindows()
        {
            if (!OperatingSystem.IsWindows()) return;
            CommandLine.WriteLine($"Uninstalling for Windows...", ConsoleColor.DarkGray);

            string[] paths =
            [
                @"*\shell\ParallelPush",
                @"*\shell\ParallelClean",
                @"*\shell\ParallelPrune",

                @"Directory\shell\ParallelPush",
                @"Directory\shell\ParallelClean",
                @"Directory\shell\ParallelPrune",

                @"Directory\Background\shell\ParallelPull",
                @"Directory\Background\shell\ParallelClean",
                @"Directory\Background\shell\ParallelPrune",
            ];

            foreach (string path in paths)
            {
                try
                {
                    Registry.ClassesRoot.DeleteSubKeyTree(path, false);
                }
                catch (Exception ex)
                {
                    CommandLine.WriteLine($"Unable to remove {path}: {ex.Message}", ConsoleColor.Yellow);
                }
            }
        }

        private void UninstallMac()
        {
            CommandLine.WriteLine($"Uninstalling for Mac...", ConsoleColor.DarkGray);
        }

        private void UninstallLinux()
        {
            CommandLine.WriteLine($"Uninstalling for Linux...", ConsoleColor.DarkGray);
        }
    }
}