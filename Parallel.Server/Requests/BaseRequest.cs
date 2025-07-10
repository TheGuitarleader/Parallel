// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Net.Sockets;
using Parallel.Server.Responses;

namespace Parallel.Server.Requests
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

        protected ObjectResponse Success()
        {
            return new ObjectResponse("Success");
        }
    }
}