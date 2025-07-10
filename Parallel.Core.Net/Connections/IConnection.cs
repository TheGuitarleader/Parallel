// Copyright 2025 Kyle Ebbinga

namespace Parallel.Core.Net.Connections
{
    public interface IConnection
    {
        Task<ServerResponse> SendRequestAsync(ServerRequest request);
    }
}