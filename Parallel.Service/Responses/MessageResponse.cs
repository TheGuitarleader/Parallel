// Copyright 2025 Kyle Ebbinga

namespace Parallel.Service.Responses
{
    public class MessageResponse
    {
        public string Message { get; set; }

        public MessageResponse(string message)
        {
            Message = message;
        }
    }
}