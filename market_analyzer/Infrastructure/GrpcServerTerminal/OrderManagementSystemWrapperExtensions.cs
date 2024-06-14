using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.GrpcServerTerminal;

public static class OrderManagementSystemWrapperExtensions
{
    public static void AddOrderManagementWrapper(this IServiceCollection services)
    {
        services.AddSingleton<IOrderManagementSystemWrapper, OrderManagementSystemWrapper>();
    }
}
