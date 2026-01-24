// Copyright 2026 Kyle Ebbinga

using System.Reflection;
using System.Runtime.InteropServices;
using Parallel.Core.IO;
using Parallel.Core.Settings;
using Parallel.Core.Utils;
using Parallel.Service.Services;
using Parallel.Service.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Parallel.Service.Extensions.Logging;
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
            //builder.Logging.AddSignalR();

            AssemblyName assembly = Assembly.GetExecutingAssembly().GetName();
            Log.Information($"{assembly.Name} v{assembly.Version}");

            // Add Windows services
            if (OperatingSystem.IsWindows())
            {
                builder.Services.AddWindowsService();
            }

            // Add Linux systemd
            if (OperatingSystem.IsLinux())
            {
                builder.Services.AddSystemd();
            }

            // Add other services
            builder.Services.AddControllers().AddNewtonsoftJson(options =>
            {
                options.SerializerSettings.Converters.Add(new StringEnumConverter { NamingStrategy = new CamelCaseNamingStrategy() });
            });
            
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.WithOrigins("http://localhost:5173", "http://127.0.0.1:5173").AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                });
            });
           
            builder.Services.AddSignalR().AddHubOptions<MessageHub>(options =>
            {
                options.EnableDetailedErrors = true;
            });
            
            builder.Services.AddSingleton<ProcessMonitor>();
            builder.Services.AddSingleton<TaskQueuer>();

            // Add background services
            builder.Services.AddHostedService<TaskQueueExecutor>();
            builder.Services.AddHostedService<TestingService>();
            //builder.Services.AddHostedService<VaultSyncService>();

            WebApplication app = builder.Build();
            app.UseRouting();
            app.UseCors();
            app.MapControllers();
            app.MapHub<MessageHub>("/hub");
            await app.RunAsync();
        }
    }
}