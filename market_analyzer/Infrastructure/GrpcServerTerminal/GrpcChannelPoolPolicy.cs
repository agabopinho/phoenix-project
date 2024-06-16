using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using System.Collections.Concurrent;

namespace Infrastructure.GrpcServerTerminal;

public class GrpcChannelPoolPolicy(ILogger<GrpcChannelPoolPolicy> logger) : PooledObjectPolicy<GrpcChannel>
{
    private int _index = -1;
    private IReadOnlyCollection<Uri>? _hosts;
    private ConcurrentDictionary<string, ConcurrentQueue<bool>>? _borrowedHosts;
    private GrpcChannelOptions? _channelOptions;

    public override GrpcChannel Create()
    {
        ValidateFallbackChannels();

        int index;
        int orignalValue;

        do
        {
            index = Interlocked.Increment(ref _index);

            if (index <= _hosts!.Count - 1)
            {
                break;
            }

            orignalValue = index;
            index = 0;
        }
        while (Interlocked.CompareExchange(ref _index, index, orignalValue) != orignalValue);

        var host = _hosts.ElementAt(index);

        logger.LogWarning("Reusing Grpc Channel {target}.", $"{host.Host}:{host.Port}");

        var channel = GrpcChannel.ForAddress(host, _channelOptions!);

        _borrowedHosts![channel.Target].Enqueue(true);

        return channel;
    }

    public override bool Return(GrpcChannel obj)
    {
        return !_borrowedHosts![obj.Target].TryDequeue(out _);
    }

    public void ConfigureFallbackChannels(IEnumerable<Uri> hosts, GrpcChannelOptions channelOptions)
    {
        _hosts = [.. hosts];
        _channelOptions = channelOptions;

        ValidateFallbackChannels();

        _borrowedHosts = new();

        foreach (var host in _hosts)
        {
            _borrowedHosts[$"{host.Host}:{host.Port}"] = new();
        }
    }

    private void ValidateFallbackChannels()
    {
        if (_hosts is null || _hosts.Count == 0)
        {
            throw new ArgumentException($"Fallback channels is empty.");
        }

        if (_channelOptions is null)
        {
            throw new ArgumentException("Channel configuration is empty.");
        }
    }
}