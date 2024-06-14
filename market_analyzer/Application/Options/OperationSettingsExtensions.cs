using Microsoft.Extensions.DependencyInjection;

namespace Application.Options;

public static class OperationSettingsExtensions
{
    public static void AddOperationSettings(this IServiceCollection services, Action<OperationOptions> configure)
    {
        services.AddOptions<OperationOptions>()
            .Configure(configure);
    }
}
