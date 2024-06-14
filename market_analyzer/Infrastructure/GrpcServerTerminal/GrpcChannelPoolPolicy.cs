using Grpc.Net.Client;
using Microsoft.Extensions.ObjectPool;

namespace Infrastructure.GrpcServerTerminal;

public class GrpcChannelPoolPolicy : PooledObjectPolicy<GrpcChannel>
{
    public override GrpcChannel Create()
    {
        throw new InvalidOperationException("No grpc port available.");
    }

    public override bool Return(GrpcChannel obj)
    {
        return true;
    }
}