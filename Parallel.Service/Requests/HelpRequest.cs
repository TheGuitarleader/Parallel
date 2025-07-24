// Copyright 2025 Kyle Ebbinga

using System.ComponentModel;
using Parallel.Core.Net.Sockets;
using Parallel.Service.Responses;

namespace Parallel.Service.Requests
{
    [Description("Lists all avalible requests to the server.")]
    public class HelpRequest : BaseRequest
    {
        public override Task<IResponse> ExecuteAsync()
        {
            throw new NotImplementedException();
        }
    }
}