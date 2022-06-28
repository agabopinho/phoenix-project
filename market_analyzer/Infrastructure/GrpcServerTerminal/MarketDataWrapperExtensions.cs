using Grpc.Terminal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.GrpcServerTerminal
{
    public static class MarketDataWrapperExtensions
    {
        public static void AddMarketDataWrapper(this IServiceCollection services, Action<MarketDataWrapperOptions> configure)
        {
            services.AddOptions<MarketDataWrapperOptions>()
                .Configure(configure)
                .Validate(options =>
                    !string.IsNullOrWhiteSpace(options.Endpoint) &&
                    Uri.TryCreate(options.Endpoint, UriKind.Absolute, out _));

            services.AddGrpcClient<MarketData.MarketDataClient>(MarketDataWrapper.ClientName, (serviceProvider, configure) =>
            {
                var options = serviceProvider.GetRequiredService<IOptionsSnapshot<MarketDataWrapperOptions>>();
                configure.Address = new Uri(options.Value.Endpoint!);
            })
            .ConfigureChannel((serviceProvider, configure) =>
                configure.MaxReceiveMessageSize = int.MaxValue);
        }
    }
}
