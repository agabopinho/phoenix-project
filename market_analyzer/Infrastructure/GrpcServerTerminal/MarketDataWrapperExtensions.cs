using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.GrpcServerTerminal;

public static class MarketDataWrapperExtensions
{
    public static void AddMarketDataWrapper(this IServiceCollection services)
    {
        services.AddSingleton<IMarketDataWrapper, MarketDataWrapper>();
    }
}
