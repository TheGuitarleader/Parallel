// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Net.Sockets;
using Parallel.Service.Responses;

namespace Parallel.Service.Requests
{
    /// <summary>
    /// The base implementation for an <see cref="IRequest"/>
    /// </summary>
    public abstract class BaseRequest : IRequest
    {
        protected ISocketHandler Handler { get; }

        public abstract Task<IResponse> ExecuteAsync();

        public virtual void Dispose()
        {
            Handler.Close();
            GC.SuppressFinalize(this);
        }

        public static MessageResponse Ok()
        {
            return new MessageResponse("Success", 200);
        }

        public static MessageResponse Ok(string message)
        {
            return new MessageResponse(message, 200);
        }

        public static ObjectResponse Json(object data)
        {
            return new ObjectResponse(data, 200);
        }

        public static MessageResponse BadRequest(string message)
        {
            return new MessageResponse(message, 401);
        }

        public static MessageResponse Unauthorized()
        {
            return new MessageResponse("Unauthorized", 401);
        }

        public static MessageResponse Forbidden()
        {
            return new MessageResponse("Forbidden", 403);
        }

        public static ErrorResponse InternalServerError(Exception exception)
        {
            return new ErrorResponse(exception, 500);
        }

        public static MessageResponse NotImplemented()
        {
            return new MessageResponse("Function not implemented", 501);
        }
    }
}