// Copyright 2025 Kyle Ebbinga

namespace Parallel.Service.Responses
{
    public class ErrorResponse : IResponse
    {
        public int Status { get; }
        public string Error { get; }

        public ErrorResponse(Exception exception, int status)
        {
            Status = status;
            Error = $"{exception.GetType().FullName}: {exception.Message}";
        }
    }
}