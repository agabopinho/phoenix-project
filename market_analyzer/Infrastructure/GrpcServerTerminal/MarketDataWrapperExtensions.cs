using Grpc.Terminal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.GrpcServerTerminal;

public static class MarketDataWrapperExtensions
{
    public static void AddMarketDataWrapper(this IServiceCollection services)
    {
        using var serviceProvider = services.BuildServiceProvider();

        var grpcServerOptions = serviceProvider.GetRequiredService<IOptionsSnapshot<GrpcServerOptions>>();

        foreach (var endpoint in grpcServerOptions.Value.MarketData ?? [])
        {
            services
                .AddGrpcClient<MarketData.MarketDataClient>(
                    endpoint.Key,
                    (serviceProvider, configure) =>
                    {
                        configure.Address = new Uri(endpoint.Value);
                    })
                .ConfigureChannel((serviceProvider, configure) =>
                    configure.MaxReceiveMessageSize = int.MaxValue);
        }

        services.AddSingleton<IMarketDataWrapper, MarketDataWrapper>();
    }
}
