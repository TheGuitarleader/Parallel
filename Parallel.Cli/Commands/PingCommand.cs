// Copyright 2025 Kyle Ebbinga

using System.CommandLine;
using Parallel.Core.Net;
using Parallel.Core.Net.Connections;
using Parallel.Core.Settings;

namespace Parallel.Cli.Commands
{
    public class PingCommand : Command
    {
        private Option<string> Address = new(["--host", "-h"], "The server address.");
        private Option<int> Port = new(["--port", "-p"], "The server port.");

        public PingCommand() : base("ping", "Tests the connection to Parallel.")
        {
            this.AddOption(Address);
            this.AddOption(Port);
            this.SetHandler(async (address, port) =>
            {
                IConnection connection = new TcpConnection(address, port);
                await connection.SendRequestAsync(new ServerRequest("ping", new Dictionary<string, string>()));
            }, Address, Port);
        }
    }
}