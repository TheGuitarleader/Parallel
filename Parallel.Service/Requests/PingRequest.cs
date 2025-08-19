// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Net.Sockets;
using Parallel.Service.Responses;

namespace Parallel.Service.Requests
{
    public class PingRequest : BaseRequest
    {
        public override Task<IResponse> ExecuteAsync()
        {
            return Task.FromResult<IResponse>(Success());
        }
    }
}