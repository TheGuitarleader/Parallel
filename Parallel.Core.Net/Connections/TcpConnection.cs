// Copyright 2025 Kyle Ebbinga

using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Parallel.Core.Utils;
using Serilog;
using Serilog.Core;

namespace Parallel.Core.Net.Connections
{
    public class TcpConnection : IConnection
    {
        private readonly string _address;
        private readonly int _port;

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerConnection"/> class with the saved settings.
        /// </summary>
        public TcpConnection()
        {
            _address = "127.0.0.1";
            _port = 8192;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ServerConnection"/> class with a address and port.
        /// </summary>
        public TcpConnection(string address, int port)
        {
            _address = address;
            _port = port;
        }

        public async Task<ServerResponse> SendRequestAsync(ServerRequest request)
        {
            Socket socket = new(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.DontLinger, true);
            ServerResponse response = new(request);

            try
            {
                socket.Connect(_address, _port);
                if (socket.Connected)
                {
                    Console.WriteLine($"Connected: {socket.Connected}");
                    Console.WriteLine($"Available: {socket.Available}");


                    // Sends an encrypted json request to the server.
                    string rawJson = JsonConvert.SerializeObject(request) + ";";
                    Log.Debug($"Sending request: '{rawJson}'");
                    socket.Send(Encoding.UTF8.GetBytes(rawJson));

                    // The encrypted returned json
                    string returnedData = string.Empty;
                    using (NetworkStream ns = new(socket))
                    {
                        while (!returnedData.EndsWith(';'))
                        {
                            byte[] buffer = new byte[socket.ReceiveBufferSize];
                            int bytesRead = ns.Read(buffer, 0, socket.ReceiveBufferSize);
                            returnedData += Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        }
                    }

                    Log.Debug($"Response: {returnedData}");
                    JToken? json = JToken.Parse(returnedData.TrimEnd(';'));
                    response = ServerResponse.Parse(request, json);

                    // Closes the socket.
                    socket.Shutdown(SocketShutdown.Both);
                    socket.Close();
                    return response;
                }
                else
                {
                    Log.Warning($"Failed to connect to server '{_address}:{_port}'");
                    return response;
                }
            }
            catch (Exception ex)
            {
                Log.Warning(ex.Message);
                return response;
            }
        }
    }
}