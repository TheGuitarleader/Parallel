// Copyright 2025 Kyle Ebbinga

using System.Net;
using System.Net.Sockets;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Parallel.Core.Events;
using Parallel.Core.Utils;

namespace Parallel.Core.Net
{
    /// <summary>
    /// Represents UDP communication between services.
    /// </summary>
    public class Communication
    {
        private readonly CancellationTokenSource _exit = new();
        private bool _active;

        /// <summary>
        /// The primary client for network communication.
        /// </summary>
        public UdpClient Client { get; } = new UdpClient();

        public event EventHandler<MessageRecievedEventArgs> RecievedMessage;

        public Communication()
        {
            Client = new UdpClient();
        }

        public Communication(int port)
        {
            Client = new UdpClient(new IPEndPoint(IPAddress.Any, port));
        }

        /// <summary>
        /// Starts listening for messages.
        /// </summary>
        public async Task Start()
        {
            _active = true;
            while (_active && !_exit.IsCancellationRequested)
            {
                UdpReceiveResult result = await Client.ReceiveAsync(_exit.Token);
                RecievedMessage?.Invoke(this, new MessageRecievedEventArgs(result));
            }
        }

        /// <summary>
        /// Stops listening for messages.
        /// </summary>
        public void Stop()
        {
            _active = false;
            _exit.Cancel();
        }

        /// <summary>
        /// Sends a message to a specified port on a specified remote host.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="endPoint"></param>
        public void Send(string message, IPEndPoint endPoint)
        {
            Client.Send(Encoding.UTF8.GetBytes(Encryption.Encode(message)), endPoint);
        }
    }
}