// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using Parallel.Core.Net;
using Parallel.Core.Net.Connections;

namespace Parallel.Cli.Commands
{
    public class PingCommand : Command
    {
        private Argument<string> Address = new("address", "The server address.");
        private Argument<int> Port = new("port", "The server port.");

        public PingCommand() : base("ping", "Tests the connection to Parallel.")
        {
            this.AddArgument(Address);
            this.AddArgument(Port);
            this.SetHandler(async (address, port) =>
            {
                IConnection connection = new TcpConnection(address, port);
                await connection.SendRequestAsync(new ServerRequest("ping", new Dictionary<string, string>()));
            }, Address, Port);
        }
    }
}