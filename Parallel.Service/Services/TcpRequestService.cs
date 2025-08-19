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
                IPAddress address = string.IsNullOrEmpty(_settings.Address) ? IPAddress.Any : IPAddress.Parse(_settings.Address);
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
            Task<IResponse> handleTask = AcceptRequestAsync(handler);
            Task timeoutTask = Task.Delay(TimeSpan.FromSeconds(30), token);

            Task wrappedTask = Task.Run(async () =>
            {
                Task completed = await Task.WhenAny(handleTask, timeoutTask);
                IResponse response;

                if (completed == handleTask)
                {
                    try
                    {
                        response = await handleTask;
                    }
                    catch (OperationCanceledException)
                    {
                        _logger.LogInformation($"[{handler.RemoteEndPoint}]: Request cancelled.");
                        response = new MessageResponse("Request cancelled", 503);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"[{handler.RemoteEndPoint}]: Handler failed.");
                        response = new ErrorResponse(ex, 500);
                    }
                }
                else
                {
                    _logger.LogWarning($"[{handler.RemoteEndPoint}]: Timed out after 30 seconds.");
                    response = new MessageResponse("Request timed out", 408);
                }

                await handler.RespondAsync(response);
                handler.Close();
            }, token);

            _requestPool.Add(wrappedTask);
        }

        private async Task<IResponse> AcceptRequestAsync(ISocketHandler handler)
        {
            ServerRequest? request = handler.Parse();
            if (request == null) return new MessageResponse("Unable to parse request", 401);

            Log.Debug($"Received request '{handler.RawData}' from '{handler.RemoteEndPoint}' ({_requestPool.Count} active request{(_requestPool.Count == 1 ? string.Empty : "s")})");
            IRequest? requestInstance = _requests.CreateNew(request);
            if (requestInstance == null) return new MessageResponse("Required fields are missing", 401);
            return await requestInstance.ExecuteAsync();
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