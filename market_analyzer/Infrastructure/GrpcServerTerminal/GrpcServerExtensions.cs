using Grpc.Net.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.ObjectPool;
using Microsoft.Extensions.Options;

namespace Infrastructure.GrpcServerTerminal;

public static class GrpcServerExtensions
{
    public static void AddGrpcServerOptions(this IServiceCollection services, Action<GrpcServerOptions> configure)
    {
        services.AddOptions<GrpcServerOptions>()
            .Configure(configure)
            .Validate(it =>
                it.Hosts is not null &&
                it.Hosts.Any());

        services.AddSingleton(serviceProvider =>
        {
            var grpcServerOptions = serviceProvider.GetRequiredService<IOptionsMonitor<GrpcServerOptions>>();

            var objectPoolProvider = new DefaultObjectPoolProvider
            {
                MaximumRetained = grpcServerOptions.CurrentValue.Hosts!.Count()
            };

            var poolPolicy = new GrpcChannelPoolPolicy();
            var objectPool = objectPoolProvider.Create(poolPolicy);

            var grpcChannelOptions = new GrpcChannelOptions
            {
                MaxReceiveMessageSize = int.MaxValue,
                MaxSendMessageSize = int.MaxValue,
            };

            foreach (var host in grpcServerOptions.CurrentValue.Hosts!)
            {
                objectPool.Return(GrpcChannel.ForAddress(host, grpcChannelOptions));
            }

            return objectPool;
        });
    }
}

