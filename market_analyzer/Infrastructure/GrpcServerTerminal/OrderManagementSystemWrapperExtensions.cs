using Grpc.Terminal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.GrpcServerTerminal;

public static class OrderManagementSystemWrapperExtensions
{
    public static void AddOrderManagementWrapper(this IServiceCollection services)
    {
        using var serviceProvider = services.BuildServiceProvider();

        var grpcServerOptions = serviceProvider.GetRequiredService<IOptionsSnapshot<GrpcServerOptions>>();

        foreach (var endpoint in grpcServerOptions.Value.OrderManagement ?? [])
        {
            services
                .AddGrpcClient<OrderManagementSystem.OrderManagementSystemClient>(
                    endpoint.Key,
                    (serviceProvider, configure) =>
                    {
                        configure.Address = new Uri(endpoint.Value);
                    })
                .ConfigureChannel((serviceProvider, configure) =>
                    configure.MaxReceiveMessageSize = int.MaxValue);
        }

        services.AddSingleton<IOrderCreator, OrderCreator>();
        services.AddSingleton<IOrderManagementSystemWrapper, OrderManagementSystemWrapper>();
    }
}
