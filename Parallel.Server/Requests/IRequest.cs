// Copyright 2025 Kyle Ebbinga

using Parallel.Core.Net;
using Parallel.Core.Net.Sockets;
using Parallel.Server.Responses;

namespace Parallel.Server.Requests
{
    /// <summary>
    /// Defines the request class.
    /// </summary>
    public interface IRequest : IDisposable
    {
        /// <summary>
        /// Executes a request and responds with an <see cref="IResponse"/>.
        /// </summary>
        Task<IResponse> ExecuteAsync();
    }
}