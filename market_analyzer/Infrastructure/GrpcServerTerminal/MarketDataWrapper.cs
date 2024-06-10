using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Microsoft.Extensions.Logging;

namespace Infrastructure.GrpcServerTerminal;

public interface IMarketDataWrapper
{
    AsyncServerStreamingCall<StreamRatesRangeReply> StreamRatesRange(
        string symbol,
        DateTime utcFromDate,
        DateTime utcToDate,
        Timeframe timeframe,
        int chunkSize,
        CancellationToken cancellationToken);

    AsyncServerStreamingCall<StreamRatesRangeReply> StreamRatesFromTicksRange(
        string symbol,
        DateTime utcFromDate,
        DateTime utcToDate,
        TimeSpan timeframe,
        int chunkSize,
        CancellationToken cancellationToken);

    AsyncServerStreamingCall<StreamTicksRangeReply> StreamTicksRange(
        string symbol,
        DateTime utcFromDate,
        DateTime utcToDate,
        CopyTicks type,
        int chunkSize,
        CancellationToken cancellationToken);

    Task<GetSymbolTickReply> GetSymbolTickAsync(string symbol, CancellationToken cancellationToken);
}

public class MarketDataWrapper : IMarketDataWrapper
{
    public static readonly string ClientName = nameof(MarketDataWrapper);

    private readonly GrpcClientFactory _grpcClientFactory;
    private readonly ILogger<MarketDataWrapper> _logger;

    public MarketDataWrapper(GrpcClientFactory grpcClientFactory, ILogger<MarketDataWrapper> logger)
    {
        _grpcClientFactory = grpcClientFactory;
        _logger = logger;
    }

    public AsyncServerStreamingCall<StreamRatesRangeReply> StreamRatesRange(
        string symbol,
        DateTime utcFromDate,
        DateTime utcToDate,
        Timeframe timeframe,
        int chunkSize,
        CancellationToken cancellationToken)
    {
        var client = _grpcClientFactory.CreateClient<MarketData.MarketDataClient>(ClientName);

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

    public AsyncServerStreamingCall<StreamRatesRangeReply> StreamRatesFromTicksRange(
        string symbol,
        DateTime utcFromDate,
        DateTime utcToDate,
        TimeSpan timeframe,
        int chunkSize,
        CancellationToken cancellationToken)
    {
        var client = _grpcClientFactory.CreateClient<MarketData.MarketDataClient>(ClientName);

        var request = new StreamRatesFromTicksRangeRequest
        {
            Symbol = symbol,
            FromDate = Timestamp.FromDateTime(utcFromDate),
            ToDate = Timestamp.FromDateTime(utcToDate),
            Timeframe = Duration.FromTimeSpan(timeframe),
            ChunkSize = chunkSize
        };

        return client.StreamRatesFromTicksRange(request, cancellationToken: cancellationToken);
    }

    public AsyncServerStreamingCall<StreamTicksRangeReply> StreamTicksRange(
        string symbol,
        DateTime utcFromDate,
        DateTime utcToDate,
        CopyTicks type,
        int chunkSize,
        CancellationToken cancellationToken)
    {
        var client = _grpcClientFactory.CreateClient<MarketData.MarketDataClient>(ClientName);

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

    public async Task<GetSymbolTickReply> GetSymbolTickAsync(string symbol, CancellationToken cancellationToken)
    {
        var client = _grpcClientFactory.CreateClient<MarketData.MarketDataClient>(ClientName);

        var request = new GetSymbolTickRequest
        {
            Symbol = symbol,
        };

        return await client.GetSymbolTickAsync(request, cancellationToken: cancellationToken);
    }
}