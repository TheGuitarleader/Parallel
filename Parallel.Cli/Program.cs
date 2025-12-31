// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using System.Diagnostics;
using System.Reflection;
using Parallel.Cli.Utils;
using Parallel.Core.IO;
using Parallel.Core.Settings;
using Parallel.Core.Utils;
using Serilog.Events;

namespace Parallel.Cli
{
    internal static class Program
    {
        private static readonly LogEventTracker EventTracker = new LogEventTracker();
        internal static ParallelConfig Settings = new ParallelConfig();

        public static async Task Main(string[] args)
        {
            if (!ParallelConfig.CanStartCliInstance())
            {
                CommandLine.WriteLine("An instance of Parallel is already running!", ConsoleColor.Yellow);
                return;
            }

            Settings = ParallelConfig.Load();
            AssemblyName assembly = Assembly.GetExecutingAssembly().GetName();
#if DEBUG
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Sink(EventTracker).WriteTo.Console().CreateLogger();
#else
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Sink(EventTracker).CreateLogger();
#endif

            Log.Information($"{assembly.Name} [Version {assembly.Version}]");
            RootCommand rootCommand = new("Parallel file manager - Easily back up and synchronize massive amounts of files, and free up drive space.");
            Type[] types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(Command)) && t.IsClass).ToArray();
            foreach (Type? type in types) rootCommand.AddCommand((Command)Activator.CreateInstance(type)!);
            await rootCommand.InvokeAsync(args);

            // Clean successful logs
            await Log.CloseAndFlushAsync();

            string logDir = Path.Combine(PathBuilder.ProgramData, "Logs");
            if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
            await File.WriteAllTextAsync(Path.Combine(logDir, $"{DateTime.Now:MM-dd-yyyy hh-mm-ss}.json"), JsonConvert.SerializeObject(EventTracker, Formatting.Indented));
            Settings.Save();
        }
    }
}