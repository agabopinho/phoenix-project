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
            var grpcServerOptions = serviceProvider.GetRequiredService<IOptionsMonitor<GrpcServerOptions>>();

            var addresses = GetAddresses(grpcServerOptions);

            var channelOptions = new GrpcChannelOptions
            {
                MaxReceiveMessageSize = int.MaxValue,
                MaxSendMessageSize = int.MaxValue,
            };

            var provider = new DefaultObjectPoolProvider
            {
                MaximumRetained = addresses.Count
            };

            var policy = serviceProvider.GetRequiredService<GrpcChannelPoolPolicy>();
            policy.ConfigureFallbackChannels(addresses, channelOptions);

            var objectPool = provider.Create(policy);

            foreach (var address in addresses)
            {
                objectPool.Return(GrpcChannel.ForAddress(address, channelOptions));
            }

            return objectPool;
        });
    }

    private static List<Uri> GetAddresses(IOptionsMonitor<GrpcServerOptions> grpcServerOptions)
    {
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

