// Copyright 2025 Kyle Ebbinga

using Microsoft.Extensions.Hosting;

namespace Parallel.Server.Services
{
    public class FileCleanupService : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.CompletedTask;
        }
    }
}