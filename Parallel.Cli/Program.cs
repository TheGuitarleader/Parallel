// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using System.Reflection;
using Parallel.Core.IO;
using Parallel.Core.Settings;

namespace Parallel.Cli
{
    internal class Program
    {
        internal static ParallelSettings Settings = new ParallelSettings();

        public static async Task Main(string[] args)
        {
            Settings = ParallelSettings.Load();
            string logFile = Path.Combine(PathBuilder.ProgramData, "Logs", $"{DateTime.Now:MM-dd-yyyy hh-mm-ss}.log");
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.File(logFile).CreateLogger();

            AssemblyName assembly = Assembly.GetExecutingAssembly().GetName();
            Log.Information($"{assembly.Name} [Version {assembly.Version}]");

            RootCommand rootCommand = new("Parallel file manager - Easily back up and synchronize massive amounts of files, and free up drive space.");
            Type[] types = Assembly.GetExecutingAssembly().GetTypes().Where(t => t.IsSubclassOf(typeof(Command)) && t.IsClass).ToArray();
            foreach (Type? type in types) rootCommand.AddCommand((Command)Activator.CreateInstance(type)!);
            await rootCommand.InvokeAsync(args);
            Settings.Save();
        }
    }
}