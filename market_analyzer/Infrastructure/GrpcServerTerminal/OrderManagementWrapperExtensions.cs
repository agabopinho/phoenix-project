using Grpc.Terminal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.GrpcServerTerminal
{
    public static class OrderManagementWrapperExtensions
    {
        public static void AddOrderManagementWrapper(this IServiceCollection services, Action<OrderManagementWrapperOptions> configure)
        {
            services.AddOptions<OrderManagementWrapperOptions>()
                .Configure(configure)
                .Validate(options =>
                    !string.IsNullOrWhiteSpace(options.Endpoint) &&
                    Uri.TryCreate(options.Endpoint, UriKind.Absolute, out _));

            services.AddGrpcClient<OrderManagement.OrderManagementClient>(OrderManagementWrapper.ClientName, (serviceProvider, configure) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<OrderManagementWrapperOptions>>();
                configure.Address = new Uri(options.Value.Endpoint!);
            })
            .ConfigureChannel((serviceProvider, configure) =>
                configure.MaxReceiveMessageSize = int.MaxValue);

            services.AddSingleton<IOrderCreator, OrderCreator>();
            services.AddSingleton<IOrderManagementWrapper, OrderManagementWrapper>();
        }
    }
}
