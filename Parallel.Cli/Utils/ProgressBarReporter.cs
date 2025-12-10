// Copyright 2025 Kyle Ebbinga

using System.Collections.Concurrent;
using Parallel.Core.Diagnostics;
using Parallel.Core.Models;

namespace Parallel.Cli.Utils
{
    public class ProgressBarReporter : IProgressReporter
    {
        private readonly ConcurrentDictionary<string, string> _lines = new();
        private readonly object _consoleLock = new();
        private int _nextLine = 0;

        // Assign a line for a file
        private int GetLine(string fileId)
        {
            if (!_lines.ContainsKey(fileId))
            {
                _lines[fileId] = (_nextLine++).ToString();
            }

            return int.Parse(_lines[fileId]);
        }

        public void Report(ProgressOperation operation, SystemFile file)
        {
            int line = GetLine(file.Id);
            string msg = $"{operation,-10} {file.Name,-30} ";

            // Optionally show progress, e.g., percentage
            if (file.LocalSize > 0 && file.RemoteSize > 0)
            {
                double percent = (double)file.RemoteSize / file.LocalSize * 100;
                msg += $"{percent:0.0}%";
            }

            lock (_consoleLock)
            {
                Console.SetCursorPosition(0, line);
                Console.Write(msg.PadRight(Console.WindowWidth));
            }
        }

        public void Reset()
        {
            lock (_consoleLock)
            {
                Console.Clear();
                _lines.Clear();
                _nextLine = 0;
            }
        }


        public void Failed(Exception exception, SystemFile file)
        {
            int line = GetLine(file.Id);
            lock (_consoleLock)
            {
                Console.SetCursorPosition(0, line);
                Console.ForegroundColor = ConsoleColor.Red;
                Console.Write($"FAILED {file.Name}: {exception.Message}".PadRight(Console.WindowWidth));
                Console.ResetColor();
            }
        }
    }
}