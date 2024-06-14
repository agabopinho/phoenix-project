using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;

namespace Infrastructure.GrpcServerTerminal;

public interface IMarketDataWrapper
{
    AsyncServerStreamingCall<RatesRangeReply> StreamRatesRangeAsync(
        string symbol,
        DateTime utcFromDate,
        DateTime utcToDate,
        Timeframe timeframe,
        int chunkSize,
        CancellationToken cancellationToken);

    AsyncServerStreamingCall<RatesRangeReply> StreamRatesRangeFromTicksAsync(
        string symbol,
        DateTime utcFromDate,
        DateTime utcToDate,
        TimeSpan timeframe,
        int chunkSize,
        CancellationToken cancellationToken);

    AsyncServerStreamingCall<TicksRangeBytesReply> StreamTicksRangeBytesAsync(
        string symbol,
        DateTime utcFromDate,
        DateTime utcToDate,
        CopyTicks type,
        int chunkSize,
        IEnumerable<string> returnFields,
        CancellationToken cancellationToken);

    Task<TicksRangeBytesReply> GetTicksRangeBytesAsync(
        string symbol,
        DateTime utcFromDate,
        DateTime utcToDate,
        CopyTicks type,
        IEnumerable<string> returnFields,
        CancellationToken cancellationToken);

    AsyncServerStreamingCall<TicksRangeReply> StreamTicksRangeAsync(
        string symbol,
        DateTime utcFromDate,
        DateTime utcToDate,
        CopyTicks type,
        int chunkSize,
        CancellationToken cancellationToken);

    Task<GetSymbolTickReply> GetSymbolTickAsync(string symbol, CancellationToken cancellationToken);
}

public class MarketDataWrapper(ObjectPool<GrpcChannel> grpcChannelPool, ILogger<MarketDataWrapper> logger) : IMarketDataWrapper
{
    private GrpcChannel? _lastChannel;

    public AsyncServerStreamingCall<RatesRangeReply> StreamRatesRangeAsync(
        string symbol,
        DateTime utcFromDate,
        DateTime utcToDate,
        Timeframe timeframe,
        int chunkSize,
        CancellationToken cancellationToken)
    {
        var (client, channel) = CreateClient();

        try
        {
            var request = new StreamRatesRangeRequest
            {
                Symbol = symbol,
                FromDate = Timestamp.FromDateTime(utcFromDate),
                ToDate = Timestamp.FromDateTime(utcToDate),
                Timeframe = timeframe,
                ChunkSize = chunkSize
            };

            return client.StreamRatesRange(request, cancellationToken: cancellationToken);
        }
        finally
        {
            if (channel is not null)
            {
                grpcChannelPool.Return(channel);
            }
        }
    }

    public AsyncServerStreamingCall<RatesRangeReply> StreamRatesRangeFromTicksAsync(
        string symbol,
        DateTime utcFromDate,
        DateTime utcToDate,
        TimeSpan timeframe,
        int chunkSize,
        CancellationToken cancellationToken)
    {
        var (client, channel) = CreateClient();

        try
        {
            var request = new StreamRatesRangeFromTicksRequest
            {
                Symbol = symbol,
                FromDate = Timestamp.FromDateTime(utcFromDate),
                ToDate = Timestamp.FromDateTime(utcToDate),
                Timeframe = Duration.FromTimeSpan(timeframe),
                ChunkSize = chunkSize
            };

            return client.StreamRatesRangeFromTicks(request, cancellationToken: cancellationToken);
        }
        finally
        {
            if (channel is not null)
            {
                grpcChannelPool.Return(channel);
            }
        }
    }

    public AsyncServerStreamingCall<TicksRangeReply> StreamTicksRangeAsync(
        string symbol,
        DateTime utcFromDate,
        DateTime utcToDate,
        CopyTicks type,
        int chunkSize,
        CancellationToken cancellationToken)
    {
        var (client, channel) = CreateClient();

        try
        {
            var request = new StreamTicksRangeRequest
            {
                Symbol = symbol,
                FromDate = Timestamp.FromDateTime(utcFromDate),
                ToDate = Timestamp.FromDateTime(utcToDate),
                Type = type,
                ChunkSize = chunkSize
            };

            return client.StreamTicksRange(request, cancellationToken: cancellationToken);
        }
        finally
        {
            if (channel is not null)
            {
                grpcChannelPool.Return(channel);
            }
        }
    }

    public AsyncServerStreamingCall<TicksRangeBytesReply> StreamTicksRangeBytesAsync(
        string symbol,
        DateTime utcFromDate,
        DateTime utcToDate,
        CopyTicks type,
        int chunkSize,
        IEnumerable<string> returnFields,
        CancellationToken cancellationToken)
    {
        var (client, channel) = CreateClient();

        try
        {
            var request = new StreamTicksRangeBytesRequest
            {
                Symbol = symbol,
                FromDate = Timestamp.FromDateTime(utcFromDate),
                ToDate = Timestamp.FromDateTime(utcToDate),
                Type = type,
                ChunkSize = chunkSize
            };

            if (returnFields is not null)
            {
                request.ReturnFields.AddRange(returnFields);
            }

            return client.StreamTicksRangeBytes(request, cancellationToken: cancellationToken);
        }
        finally
        {
            if (channel is not null)
            {
                grpcChannelPool.Return(channel);
            }
        }
    }

    public async Task<TicksRangeBytesReply> GetTicksRangeBytesAsync(
        string symbol,
        DateTime utcFromDate,
        DateTime utcToDate,
        CopyTicks type,
        IEnumerable<string> returnFields,
        CancellationToken cancellationToken)
    {
        var (client, channel) = CreateClient();

        try
        {
            var request = new GetTicksRangeBytesRequest
            {
                Symbol = symbol,
                FromDate = Timestamp.FromDateTime(utcFromDate),
                ToDate = Timestamp.FromDateTime(utcToDate),
                Type = type,
            };

            if (returnFields is not null)
            {
                request.ReturnFields.AddRange(returnFields);
            }

            return await client.GetTicksRangeBytesAsync(request, cancellationToken: cancellationToken);
        }
        finally
        {
            if (channel is not null)
            {
                grpcChannelPool.Return(channel);
            }
        }
    }

    public async Task<GetSymbolTickReply> GetSymbolTickAsync(string symbol, CancellationToken cancellationToken)
    {
        var (client, channel) = CreateClient();

        try
        {
            var request = new GetSymbolTickRequest
            {
                Symbol = symbol,
            };

            return await client.GetSymbolTickAsync(request, cancellationToken: cancellationToken);
        }
        finally
        {
            if (channel is not null)
            {
                grpcChannelPool.Return(channel);
            }
        }
    }

    private (MarketData.MarketDataClient client, GrpcChannel? channel) CreateClient()
    {
        var channel = grpcChannelPool.Get();

        if (channel is null && _lastChannel is not null)
        {
            logger.LogWarning("Reusing last grpc channel.");

            return (new MarketData.MarketDataClient(_lastChannel), null);
        }

        if (channel is null)
        {
            throw new InvalidOperationException("Error in channel configuration.");
        }

        Interlocked.Exchange(ref _lastChannel, channel);

        return (new MarketData.MarketDataClient(channel), channel);
    }
}