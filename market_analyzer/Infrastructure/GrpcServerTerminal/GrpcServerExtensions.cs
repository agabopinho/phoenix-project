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

        services.AddTransient<GrpcChannelPoolPolicy>();

        services.AddSingleton(serviceProvider =>
        {
            var addresses = GetAddresses(serviceProvider);

            var policy = serviceProvider.GetRequiredService<GrpcChannelPoolPolicy>();

            policy.ConfigureChannels(addresses, new GrpcChannelOptions
            {
                MaxReceiveMessageSize = int.MaxValue,
                MaxSendMessageSize = int.MaxValue,
            });

            var provider = new DefaultObjectPoolProvider
            {
                MaximumRetained = addresses.Count
            };

            return provider.Create(policy);
        });
    }

    private static List<Uri> GetAddresses(IServiceProvider serviceProvider)
    {
        var grpcServerOptions = serviceProvider.GetRequiredService<IOptionsMonitor<GrpcServerOptions>>();

        var addresses = new List<Uri>();

        foreach (var host in grpcServerOptions.CurrentValue.Hosts!)
        {
            var parts = host.Split("+", StringSplitOptions.RemoveEmptyEntries);
            var hostPart = new Uri(parts[0]);
            var quantityParty = parts.Length > 1 ? int.Parse(parts[1]) : 0;

            quantityParty += 1;

            var port = hostPart.Port;

            for (var i = 0; i < quantityParty; i++)
            {
                var channelHost = new Uri($"{hostPart.Scheme}://{hostPart.Host}:{port}");

                addresses.Add(channelHost);

                port += 1;
            }
        }

        return addresses;
    }
}

