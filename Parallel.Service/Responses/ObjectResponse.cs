// Copyright 2025 Kyle Ebbinga

namespace Parallel.Service.Responses
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