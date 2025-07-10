// Copyright 2025 Kyle Ebbinga

using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Parallel.Core.Utils;
using Serilog;
using Serilog.Core;

namespace Parallel.Core.Net.Sockets
{
    public class TcpSocketHandler : ISocketHandler
    {
        /// <summary>
        ///
        /// </summary>
        public Socket Socket { get; }

        /// <inheritdoc />
        public UnixTime ReceivedAt { get; }

        /// <inheritdoc />
        public string RawData { get; set; } = string.Empty;

        /// <inheritdoc />
        public IPEndPoint RemoteEndPoint { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TcpSocketHandler"/> class for the specified socket.
        /// </summary>
        /// <param name="socket">The socket to handle.</param>
        public TcpSocketHandler(Socket socket)
        {
            ReceivedAt = UnixTime.Now;
            Socket = socket;
            RemoteEndPoint = (IPEndPoint)socket.RemoteEndPoint;
        }

        public void Close()
        {
            Socket.Shutdown(SocketShutdown.Both);
            Socket.Close();
        }

        public ServerRequest? Parse()
        {
            using (NetworkStream ns = new(Socket))
            {
                Console.WriteLine("1");
                while (!RawData.EndsWith(';'))
                {
                    Console.WriteLine("2");
                    byte[] buffer = new byte[Socket.ReceiveBufferSize];
                    int bytesRead = ns.Read(buffer, 0, Socket.ReceiveBufferSize);
                    RawData += Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Log.Debug(RawData);
                    Console.WriteLine("3");
                }

                Console.WriteLine("4");
            }

            return JsonConvert.DeserializeObject<ServerRequest>(RawData.TrimEnd(';'));
        }

        public Task RespondAsync(object? data)
        {
            try
            {
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                byte[] bytes = Encoding.ASCII.GetBytes(Encryption.Encode(json));
                Socket?.Send(bytes);
                Close();
                return Task.CompletedTask;
            }
            catch (ObjectDisposedException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
                return Task.CompletedTask;
            }
        }
    }
}