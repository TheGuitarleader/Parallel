// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using System.Reflection;
using Parallel.Core.IO;
using Parallel.Core.Settings;

namespace Parallel.Cli
{
    internal class Program
    {
        internal static ParallelConfig Settings = new ParallelConfig();

        public static async Task Main(string[] args)
        {
            Settings = ParallelConfig.Load();
            string logFile = Path.Combine(PathBuilder.ProgramData, "Logs", "latest.txt");
            if (File.Exists(logFile)) File.Delete(logFile);
#if DEBUG
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().CreateLogger();
#else
            Log.Logger = new LoggerConfiguration().MinimumLevel.Warning().WriteTo.File(logFile).CreateLogger();
#endif

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