// Copyright 2025 Kyle Ebbinga

using System.Net;
using System.Net.Sockets;
using Parallel.Core.Utils;

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
        public string RawData { get; set; }

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

        public ServerRequest ReceiveRequest()
        {
            throw new NotImplementedException();
        }
    }
}