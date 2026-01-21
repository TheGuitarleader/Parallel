// Copyright 2026 Kyle Ebbinga

using Grpc.Core;
using Parallel.Service;

namespace Parallel.Service.Services;

public class GrpcService : Greeter.GreeterBase
{
    private readonly ILogger<GrpcService> _logger;

    public GrpcService(ILogger<GrpcService> logger)
    {
        _logger = logger;
    }

    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
        _logger.LogDebug("Received HelloRequest");
        return Task.FromResult(new HelloReply
        {
            Message = "Hello " + request.Name
        });
    }
}