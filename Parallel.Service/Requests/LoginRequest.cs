// Copyright 2025 Kyle Ebbinga

using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Parallel.Core.Net.Sockets;
using Parallel.Service.Responses;

namespace Parallel.Service.Requests
{
    [Description("Logins into the server.")]
    public class LoginRequest : BaseRequest
    {
        [Required] public string Username { get; set; }

        [Required] public string Password { get; set; }

        public override Task<IResponse> ExecuteAsync()
        {
            return Task.FromResult<IResponse>(Success());
        }
    }
}