// Copyright 2025 Kyle Ebbinga

namespace Parallel.Core.Net.Connections
{
    public interface IConnection
    {
        ServerResponse SendRequest(ServerRequest request);
        //Task<ServerResponse> SendRequestAsync(ServerRequest request);
    }
}