// Copyright 2025 Kyle Ebbinga

using Microsoft.Extensions.Hosting;
using Parallel.Core.IO;

namespace Parallel.Service.Services
{
    public class LoggingService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(GetTimeUntilNextDay(), stoppingToken);
                await Log.CloseAndFlushAsync();

                File.Move(Program.LogFile, Path.Combine(PathBuilder.ProgramData, "Logs", $"{DateTime.Now:MM-dd-yyyy hh-mm-ss}.log"));
            }
        }

        private static TimeSpan GetTimeUntilNextDay()
        {
            DateTime current = DateTime.Now;
            DateTime nextMidnight = current.AddDays(1);
            return nextMidnight - current;
        }
    }
}