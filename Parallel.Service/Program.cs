// Copyright 2026 Kyle Ebbinga

using System.Reflection;
using System.Runtime.InteropServices;
using Parallel.Core.IO;
using Parallel.Core.Settings;
using Parallel.Core.Utils;
using Parallel.Service.Services;
using Parallel.Service.Tasks;
using Serilog;

namespace Parallel.Service
{
    public class Program
    {
        private static readonly LogEventTracker EventTracker = new LogEventTracker();
        internal static readonly string LogFile = Path.Combine(PathBuilder.ProgramData, "Logs", "latest.txt");

        public static async Task Main(string[] args)
        {
            WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

            // Add Logging services
            Log.Logger = new LoggerConfiguration().MinimumLevel.Debug().WriteTo.Console().WriteTo.File(LogFile).CreateLogger();
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

            // Add other services
            builder.Services.AddGrpc();
            builder.Services.AddGrpcReflection();
            builder.Services.AddSingleton(ParallelConfig.Load());
            builder.Services.AddSingleton<ProcessMonitor>();
            builder.Services.AddSingleton<TaskQueuer>();

            // Add background services
            builder.Services.AddHostedService<TaskQueueExecuter>();

            WebApplication app = builder.Build();
            app.MapGet("/", () => "Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
            app.MapGrpcService<GrpcService>();
            app.MapGrpcReflectionService();

            IHostApplicationLifetime lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();
            ParallelConfig settings = app.Services.GetRequiredService<ParallelConfig>();
            lifetime.ApplicationStopped.Register(() =>
            {
                settings.Save();
            });

            await app.RunAsync();
        }
    }
}