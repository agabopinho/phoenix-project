using Grpc.Net.Client;
using Microsoft.Extensions.ObjectPool;

namespace Infrastructure.GrpcServerTerminal;

public class GrpcChannelPoolPolicy : PooledObjectPolicy<GrpcChannel>
{
    public override GrpcChannel Create()
    {
        return null!;
    }

    public override bool Return(GrpcChannel obj)
    {
        return true;
    }
}