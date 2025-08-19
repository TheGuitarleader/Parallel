// Copyright 2025 Kyle Ebbinga

using Newtonsoft.Json.Linq;

namespace Parallel.Core.Net.Connections
{
    public class ServerResponse
    {
        public ServerRequest Request { get; }
        public bool IsSuccess { get; } = false;
        public JToken? Data { get; }

        public ServerResponse(ServerRequest request)
        {
            Request = request;
        }

        private ServerResponse(ServerRequest request, JToken? data, bool isSuccess)
        {
            Request = request;
            IsSuccess = isSuccess;
            Data = data;
        }

        public static ServerResponse Parse(ServerRequest request, JToken? json)
        {
            return new ServerResponse(request, json, json != null);
        }
    }
}