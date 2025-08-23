// Copyright 2025 Kyle Ebbinga

namespace Parallel.Service.Responses
{
    public class MessageResponse : IResponse
    {
        public int Status { get; }
        public string Message { get; }

        public MessageResponse(string message, int status)
        {
            Message = message;
            Status = status;
        }
    }
}