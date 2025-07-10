// Copyright 2025 Kyle Ebbinga

using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Parallel.Core.IO;
using Parallel.Core.Settings;
using Parallel.Core.Utils;
using Parallel.Server.Requests;
using Parallel.Server.Services;

namespace Parallel.Server
{
    internal class Program
    {
        private static string _logFile = Path.Combine(PathBuilder.ProgramData, "Logs", "latest.txt");

        static async Task Main(string[] args)
        {
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

            // Logging
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().WriteTo.File(_logFile).CreateLogger();
            builder.Logging.ClearProviders();
            builder.Logging.AddSerilog();

            AssemblyName assembly = Assembly.GetExecutingAssembly().GetName();
            Log.Information($"{assembly.Name} v{assembly.Version}");

            // Add Windows services
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                builder.Services.AddWindowsService();
            }

            // Add Linux systemd
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                builder.Services.AddSystemd();
            }

            // Background services
            builder.Services.AddHostedService<FileBackupService>();
            builder.Services.AddHostedService<FileCleanupService>();
            builder.Services.AddHostedService<TcpRequestService>();

            // Other services
            builder.Services.AddSingleton(ParallelSettings.Load());
            builder.Services.AddSingleton<RequestHandler>();
            builder.Services.AddSingleton<ProcessMonitor>();

            IHost host = builder.Build();
            IHostApplicationLifetime lifetime = host.Services.GetRequiredService<IHostApplicationLifetime>();
            ParallelSettings settings = host.Services.GetRequiredService<ParallelSettings>();
            lifetime.ApplicationStopped.Register(() =>
            {
                settings.Save();
                Log.CloseAndFlush();
                File.Move(_logFile, Path.Combine(PathBuilder.ProgramData, "Logs", $"{DateTime.Now:MM-dd-yyyy hh-mm-ss}.log"));
            });

            // Starts the application
            await host.RunAsync();
        }
    }
}