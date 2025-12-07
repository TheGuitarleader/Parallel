// Copyright 2025 Kyle Ebbinga

namespace Parallel.Service.Responses
{
    public sealed class ObjectResponse : IResponse
    {
        public int Status { get; }
        public object? Data { get; }

        public ObjectResponse(object? data, int status)
        {
            Status = status;
            Data = data;
        }
    }
}