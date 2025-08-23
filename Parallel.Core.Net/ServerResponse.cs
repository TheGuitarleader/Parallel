// Copyright 2025 Kyle Ebbinga

using Newtonsoft.Json.Linq;

namespace Parallel.Core.Net.Connections
{
    public class ServerResponse
    {
        public ServerRequest Request { get; }
        public int StatusCode { get; }
        public bool Success { get; } = false;
        public JToken? Data { get; }
        public string? Message { get; }
        public string? Error { get; }

        public ServerResponse(ServerRequest request)
        {
            Request = request;
        }

        private ServerResponse(ServerRequest request, JToken? data, string? message, string? error, int statusCode)
        {
            Request = request;
            StatusCode = statusCode;
            Success = statusCode == 200;
            Data = data;
            Message = message;
            Error = error;
        }

        public static ServerResponse Parse(ServerRequest request, JToken? json)
        {
            int statusCode = json?["status"]?.Value<int>() ?? 408;
            JToken? data = json?["data"];
            string? message = json?.Value<string>("message");
            string? error = json?.Value<string>("error");

            return new ServerResponse(request, data, message, error, statusCode);
        }
    }
}