using Grpc.Terminal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.GrpcServerTerminal;

public static class OrderManagementSystemWrapperExtensions
{
    public static void AddOrderManagementWrapper(this IServiceCollection services, Action<OrderManagementSystemWrapperOptions> configure)
    {
        services.AddOptions<OrderManagementSystemWrapperOptions>()
            .Configure(configure)
            .Validate(options =>
                !string.IsNullOrWhiteSpace(options.Endpoint) &&
                Uri.TryCreate(options.Endpoint, UriKind.Absolute, out _));

        services
            .AddGrpcClient<OrderManagementSystem.OrderManagementSystemClient>(
                OrderManagementSystemWrapper.ClientName,
                (serviceProvider, configure) =>
                {
                    var options = serviceProvider.GetRequiredService<IOptions<OrderManagementSystemWrapperOptions>>();
                    configure.Address = new Uri(options.Value.Endpoint!);
                })
            .ConfigureChannel((serviceProvider, configure) =>
                configure.MaxReceiveMessageSize = int.MaxValue);

        services.AddSingleton<IOrderCreator, OrderCreator>();
        services.AddSingleton<IOrderManagementSystemWrapper, OrderManagementSystemWrapper>();
    }
}
