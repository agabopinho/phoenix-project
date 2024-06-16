using Grpc.Net.Client;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using System.Collections.Concurrent;

namespace Infrastructure.GrpcServerTerminal;

public class GrpcChannelPoolPolicy(ILogger<GrpcChannelPoolPolicy> logger) : PooledObjectPolicy<GrpcChannel>
{
    private int _index = -1;
    private IReadOnlyCollection<Uri>? _hosts;
    private GrpcChannelOptions? _channelOptions;

    private readonly ConcurrentQueue<Uri> _channels = new();
    private readonly ConcurrentDictionary<string, ConcurrentQueue<bool>> _borrowedHosts = new();

    public override GrpcChannel Create()
    {
        ValidateChannels();

        if (_channels.TryDequeue(out var host))
        {
            return GrpcChannel.ForAddress(host, _channelOptions!);
        }

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

        host = _hosts.ElementAt(index);

        logger.LogWarning("Reusing Grpc Channel {target}.", $"{host.Host}:{host.Port}");

        var channel = GrpcChannel.ForAddress(host, _channelOptions!);

        _borrowedHosts![channel.Target].Enqueue(true);

        return channel;
    }

    public override bool Return(GrpcChannel obj)
    {
        return !_borrowedHosts[obj.Target].TryDequeue(out _);
    }

    public void ConfigureChannels(IEnumerable<Uri> hosts, GrpcChannelOptions channelOptions)
    {
        _hosts = [.. hosts];
        _channelOptions = channelOptions;

        ValidateChannels();

        foreach (var host in _hosts)
        {
            _channels.Enqueue(host);
        }

        foreach (var host in _hosts)
        {
            _borrowedHosts[$"{host.Host}:{host.Port}"] = new();
        }
    }

    private void ValidateChannels()
    {
        if (_hosts is null || _hosts.Count == 0)
        {
            throw new ArgumentException($"Channels is empty.");
        }

        if (_channelOptions is null)
        {
            throw new ArgumentException("Channel options is empty.");
        }
    }
}