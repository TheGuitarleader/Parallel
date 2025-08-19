// Copyright 2025 Kyle Ebbinga

using System.Net;
using Parallel.Core.Utils;

namespace Parallel.Core.Net.Sockets
{
    public interface ISocketHandler
    {
        /// <summary>
        /// The time the socket was received.
        /// </summary>
        UnixTime ReceivedAt { get; }

        /// <summary>
        /// The raw string of incoming data decrypted.
        /// </summary>
        string RawData { get; set; }

        /// <summary>
        /// The remote client that sent the request.
        /// </summary>
        IPEndPoint RemoteEndPoint { get; }

        /// <summary>
        /// Shuts down the <see cref="System.Net.Sockets.Socket"/>, closes the connection, and releases all resources.
        /// </summary>
        void Close();

        /// <summary>
        /// Reads the incoming encrypted data as formatted JSON string.
        /// </summary>
        /// <returns></returns>
        ServerRequest? Parse();

        /// <summary>
        /// Responds to the current request.
        /// </summary>
        /// <param name="data"></param>
        Task RespondAsync(object? data);
    }
}