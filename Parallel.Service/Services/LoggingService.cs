// Copyright 2026 Kyle Ebbinga

using Parallel.Core.IO;
using Parallel.Core.Utils;
using Serilog;

namespace Parallel.Service.Services
{
    public class LoggingService : BackgroundService
    {
        private readonly LogEventTracker _logEventTracker;

        public LoggingService(LogEventTracker logEventTracker)
        {
            _logEventTracker = logEventTracker;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(GetTimeUntilNextDay(), stoppingToken);
                await Log.CloseAndFlushAsync();

                if (_logEventTracker.Errors.Count > 0)
                {
                    string logDir = Path.Combine(PathBuilder.ProgramData, "Logs");
                    if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);
                    File.Move(Program.LogFile, Path.Combine(logDir, $"{DateTime.Now:MM-dd-yyyy hh-mm-ss}.log"));
                }
            }
        }

        private static TimeSpan GetTimeUntilNextDay()
        {
            DateTime current = DateTime.UtcNow;
            DateTime nextMidnight = current.AddDays(1);
            return nextMidnight - current;
        }
    }
}