using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.GrpcServerTerminal;

public static class GrpcServerExtensions
{
    public static void AddGrpcServerOptions(this IServiceCollection services, Action<GrpcServerOptions> configure)
    {
        services.AddOptions<GrpcServerOptions>()
            .Configure(configure);
    }
}
