// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using System.Reflection;
using Parallel.Core.IO;
using Parallel.Core.Settings;
using Parallel.Core.Utils;
using Serilog.Events;

namespace Parallel.Cli
{
    internal class Program
    {
        private static readonly LogEventTracker EventTracker = new LogEventTracker();
        internal static ParallelConfig Settings = new ParallelConfig();

        public static async Task Main(string[] args)
        {
            Settings = ParallelConfig.Load();
            AssemblyName assembly = Assembly.GetExecutingAssembly().GetName();
            Log.Information($"{assembly.Name} [Version {assembly.Version}]");
#if DEBUG
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Sink(EventTracker).WriteTo.Console().CreateLogger();
#else
            Log.Logger = new LoggerConfiguration().WriteTo.Sink(EventTracker).CreateLogger();
#endif

            RootCommand rootCommand = new("Parallel file manager - Easily back up and synchronize massive amounts of files, and free up drive space.");
            Type[] types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(Command)) && t.IsClass).ToArray();
            foreach (Type? type in types) rootCommand.AddCommand((Command)Activator.CreateInstance(type)!);
            await rootCommand.InvokeAsync(args);

            // Clean successful logs
            await Log.CloseAndFlushAsync();
            if (EventTracker.Warnings.Count > 0 || EventTracker.Errors.Count > 0)
            {
                await File.WriteAllTextAsync(Path.Combine(PathBuilder.ProgramData, "Logs", $"{DateTime.Now:MM-dd-yyyy hh-mm-ss}.json"), JsonConvert.SerializeObject(EventTracker, Formatting.Indented));
            }

            Settings.Save();
        }
    }
}