// Copyright 2025 Kyle Ebbinga

namespace Parallel.Server.Responses
{
    public sealed class ObjectResponse : IResponse
    {
        public object? Data { get; }

        public ObjectResponse(object? data)
        {
            Data = data;
        }
    }
}