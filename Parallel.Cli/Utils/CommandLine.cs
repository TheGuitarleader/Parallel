﻿// Copyright 2025 Kyle Ebbinga

using System.Text;
using Parallel.Core.Security;
using Parallel.Core.Settings;
using Parallel.Core.Utils;

namespace Parallel.Cli.Utils
{
    public class CommandLine
    {
        private static readonly object _consoleLock = new();

        public static string ReadString(object value, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            Console.Write($"> {value}: ");
            Console.ResetColor();
            return Console.ReadLine() ?? string.Empty;
        }

        public static bool ReadBool(object value, bool defaultValue, ConsoleColor color = ConsoleColor.Gray)
        {
            HashSet<string> trueValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "y", "yes", "true", "1" };
            HashSet<string> falseValues = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "n", "no", "false", "0" };

            Console.ForegroundColor = color;
            Console.Write($"> {value}: ");

            bool result = defaultValue;
            string? input = Console.ReadLine()?.Trim();
            if (!string.IsNullOrEmpty(input) && trueValues.Contains(input))
            {
                result = true;
            }

            if (!string.IsNullOrEmpty(input) && falseValues.Contains(input))
            {
                result = false;
            }

            Console.ResetColor();
            return result;
        }

        public static string? ReadPassword(object value, ConsoleColor color = ConsoleColor.Gray)
        {
            string password = string.Empty;
            Console.ForegroundColor = color;
            Console.Write($"> {value}: ");
            Console.ResetColor();
            ConsoleKeyInfo key;

            do
            {
                key = Console.ReadKey(true);
                if (key.Key != ConsoleKey.Enter && key.Key != ConsoleKey.Backspace)
                {
                    password += key.KeyChar;
                }
                else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
                {
                    password = password.Substring(0, password.Length - 1);
                }
            } while (key.Key != ConsoleKey.Enter);

            Console.WriteLine();
            return Encryption.Encode(password);
        }

        public static void Write(object value, ConsoleColor color = ConsoleColor.Gray)
        {
            Console.ForegroundColor = color;
            Console.Write("\r" + value?.ToString()?.PadRight(Console.WindowWidth));
            Console.ResetColor();
        }

        public static void WriteLine(LocalVaultConfig localVault, object value, ConsoleColor color = ConsoleColor.Gray)
        {
            string baseLog = $"[{localVault.Id}] {value}";
            switch(color)
            {
                default:
                    Log.Information(baseLog);
                    break;

                case ConsoleColor.Yellow:
                    Log.Warning(baseLog);
                    break;

                case ConsoleColor.Red:
                    Log.Error(baseLog);
                    break;
            }

            lock (_consoleLock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine($"> {baseLog}");
                Console.ResetColor();
            }
        }

        public static void WriteLine(object value, ConsoleColor color = ConsoleColor.Gray)
        {
            string baseLog = $"{value}";
            switch(color)
            {
                default:
                    Log.Information(baseLog);
                    break;

                case ConsoleColor.Yellow:
                    Log.Warning(baseLog);
                    break;

                case ConsoleColor.Red:
                    Log.Error(baseLog);
                    break;
            }

            lock (_consoleLock)
            {
                Console.ForegroundColor = color;
                Console.WriteLine($"> {baseLog}");
                Console.ResetColor();
            }
        }

        public static void ProgressBar(double part, double total, TimeSpan elapsed, ConsoleColor color = ConsoleColor.Gray)
        {
            double percent = part / total;
            string percentStr = $"> Progress: {Convert.ToInt32(percent * 100).ToString("D2")}%";

            TimeSpan remaining;
            double remainingMs = elapsed.TotalMilliseconds * (total - part) / part;
            if (remainingMs <= TimeSpan.MaxValue.TotalMilliseconds)
                remaining = TimeSpan.FromMilliseconds(remainingMs);
            else
                remaining = TimeSpan.MaxValue;

            string remainingStr = $"{remaining.Hours:00}:{remaining.Minutes:00}:{remaining.Seconds:00} remaining";
            int barWidth = Console.WindowWidth - percentStr.Length - remainingStr.Length - 4;
            int filledWidth = Convert.ToInt32(percent * barWidth);

            StringBuilder progressBar = new StringBuilder(barWidth);
            for (int i = 0; i < filledWidth; i++)
            {
                progressBar.Append('#');
            }

            for (int i = filledWidth; i < barWidth; i++)
            {
                progressBar.Append('.');
            }

            Console.Write($"\r{percentStr} [{progressBar.ToString()}] {remainingStr}");
        }
    }
}