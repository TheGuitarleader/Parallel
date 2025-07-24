// Copyright 2025 Kyle Ebbinga

using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Parallel.Core.Net;
using Parallel.Core.Net.Sockets;
using Parallel.Core.Settings;
using Parallel.Service.Requests;
using Parallel.Service.Responses;

namespace Parallel.Service.Services
{
    public class TcpRequestService : BackgroundService
    {
        // Privates
        private readonly CancellationTokenSource _exit = new();
        private readonly ILogger<TcpRequestService> _logger;
        private readonly Socket _listener = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        private readonly ParallelSettings _settings;
        private readonly RequestHandler _requests;
        private readonly List<Task> _requestPool = new List<Task>();

        public TcpRequestService(ILogger<TcpRequestService> logger, ParallelSettings settings, RequestHandler requests)
        {
            _logger = logger;
            _settings = settings;
            _requests = requests;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                // Starts listening for requests over the TCP socket.
                IPAddress address = string.IsNullOrEmpty(_settings.Address) ? IPAddress.Any : IPAddress.Parse( _settings.Address);
                _listener.Bind(new IPEndPoint(address, _settings.ListenerPort));
                _listener.Listen(5);
            }
            catch
            {
                _logger.LogError("Failed to start server! This usually means either the port is already in use or another instance of Parallel is currently running.");
                Environment.Exit(1);
            }

            // Starts listening for connections
            _logger.LogInformation($"Listening for commands on: {_listener.LocalEndPoint}");
            while (!stoppingToken.IsCancellationRequested && !_exit.IsCancellationRequested)
            {
                _requestPool.RemoveAll(c => c.IsCompleted);
                Socket requestSocket = await _listener.AcceptAsync(_exit.Token);
                StartHandlingRequests(requestSocket, stoppingToken);
            }
        }

        private void StartHandlingRequests(Socket socket, CancellationToken token)
        {
            TcpSocketHandler handler = new(socket);
            Task handlerTask = Task.Run(() => AcceptRequestAsync(handler).ContinueWith(t => { t.Dispose(); }, token), token);
            _requestPool.Add(handlerTask);
        }

        private async Task AcceptRequestAsync(ISocketHandler handler)
        {
            ServerRequest request = handler.Parse();
            Log.Debug($"Received request '{handler.RawData}' from '{handler.RemoteEndPoint}' ({_requestPool.Count} active request{(_requestPool.Count == 1 ? string.Empty : "s")})");
            IRequest requestInstance = _requests.CreateNew(request);

            IResponse response = await requestInstance.ExecuteAsync();
            await handler.RespondAsync(response);
            Log.Debug($"Responding to '{handler.RemoteEndPoint}' with '{JsonConvert.SerializeObject(response)}' ({_requestPool.Count} active request{(_requestPool.Count == 1 ? string.Empty : "s")})");
        }

        public override async Task<Task> StopAsync(CancellationToken cancellationToken)
        {
            // Stops listening for requests.
            await _exit.CancelAsync();

            // Checks if any requests are still being processed.
            _requestPool.RemoveAll(c => c.IsCompleted);
            if (_requestPool.Count > 0) _logger.LogInformation($"Shutdown received. Still processing {_requestPool.Count} request{(_requestPool.Count == 1 ? string.Empty : "s")}!");
            await Task.WhenAll(_requestPool);

            return base.StopAsync(cancellationToken);
        }
    }
}