// Copyright 2025 Kyle Ebbinga

using Microsoft.Extensions.Hosting;
using Parallel.Shell;
using Parallel.Shell.Extensions;

namespace Parallel.Service.Services
{
    public class WindowsShellService : BackgroundService
    {
        private readonly ServerManager _manager = new();

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("RegisterServers");
            _manager.RegisterServers();
            return Task.CompletedTask;
        }

        public override Task StopAsync(CancellationToken stoppingToken)
        {
            Console.WriteLine("UnregisterServers");
            _manager.UnregisterServers();
            return Task.CompletedTask;
        }
    }
}