using Grpc.Terminal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using System.Runtime.Serialization;

namespace Infrastructure.Terminal
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
            .ConfigureChannel(configure => 
            {
                configure.MaxReceiveMessageSize = int.MaxValue;
            });
        }
    }
}
