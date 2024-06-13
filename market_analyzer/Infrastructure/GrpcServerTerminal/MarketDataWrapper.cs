using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Terminal;
using Grpc.Terminal.Enums;

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

public class MarketDataWrapper(GrpcClientFactory clientFactory) : IMarketDataWrapper
{
    public AsyncServerStreamingCall<RatesRangeReply> StreamRatesRangeAsync(
        string symbol,
        DateTime utcFromDate,
        DateTime utcToDate,
        Timeframe timeframe,
        int chunkSize,
        CancellationToken cancellationToken)
    {
        var client = clientFactory.CreateClient<MarketData.MarketDataClient>(ClientName(nameof(StreamRatesRangeAsync)));

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

    public AsyncServerStreamingCall<RatesRangeReply> StreamRatesRangeFromTicksAsync(
        string symbol,
        DateTime utcFromDate,
        DateTime utcToDate,
        TimeSpan timeframe,
        int chunkSize,
        CancellationToken cancellationToken)
    {
        var client = clientFactory.CreateClient<MarketData.MarketDataClient>(ClientName(nameof(StreamRatesRangeFromTicksAsync)));

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

    public AsyncServerStreamingCall<TicksRangeReply> StreamTicksRangeAsync(
        string symbol,
        DateTime utcFromDate,
        DateTime utcToDate,
        CopyTicks type,
        int chunkSize,
        CancellationToken cancellationToken)
    {
        var client = clientFactory.CreateClient<MarketData.MarketDataClient>(ClientName(nameof(StreamTicksRangeAsync)));

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

    public AsyncServerStreamingCall<TicksRangeBytesReply> StreamTicksRangeBytesAsync(
        string symbol,
        DateTime utcFromDate,
        DateTime utcToDate,
        CopyTicks type,
        int chunkSize,
        IEnumerable<string> returnFields,
        CancellationToken cancellationToken)
    {
        var client = clientFactory.CreateClient<MarketData.MarketDataClient>(ClientName(nameof(StreamTicksRangeBytesAsync)));

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

    public async Task<TicksRangeBytesReply> GetTicksRangeBytesAsync(
        string symbol,
        DateTime utcFromDate,
        DateTime utcToDate,
        CopyTicks type,
        IEnumerable<string> returnFields,
        CancellationToken cancellationToken)
    {
        var client = clientFactory.CreateClient<MarketData.MarketDataClient>(ClientName(nameof(GetTicksRangeBytesAsync)));

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

    public async Task<GetSymbolTickReply> GetSymbolTickAsync(string symbol, CancellationToken cancellationToken)
    {
        var client = clientFactory.CreateClient<MarketData.MarketDataClient>(ClientName(nameof(GetSymbolTickAsync)));

        var request = new GetSymbolTickRequest
        {
            Symbol = symbol,
        };

        return await client.GetSymbolTickAsync(request, cancellationToken: cancellationToken);
    }

    private static string ClientName(string name)
    {
        return name.Replace("Async", string.Empty);
    }
}