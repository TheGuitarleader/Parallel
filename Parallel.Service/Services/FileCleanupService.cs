// Copyright 2025 Kyle Ebbinga

using Microsoft.Extensions.Hosting;

namespace Parallel.Service.Services
{
    public class FileCleanupService : BackgroundService
    {
        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            throw new NotImplementedException();
        }
    }
}