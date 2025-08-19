// Copyright 2025 Kyle Ebbinga

namespace Parallel.Service.Responses
{
    public class ErrorResponse : IResponse
    {
        public int Status { get; }
        public string? Exception { get; }
        public string Message { get; }

        public ErrorResponse(Exception exception, int status)
        {
            Status = status;
            Exception = exception.GetType().FullName;
            Message = exception.Message;
        }
    }
}