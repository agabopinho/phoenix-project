using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.Client;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
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

    Task<RatesRangeReply> GetRatesRangeFromTicksAsync(
        string symbol,
        DateTime utcFromDate,
        DateTime utcToDate,
        TimeSpan timeframe,
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

    Task<PredictReply> PredictAsync(string modelName, double currentChange, CancellationToken cancellationToken);
}

public class MarketDataWrapper(ObjectPool<GrpcChannel> grpcChannelPool) : IMarketDataWrapper
{
    public const string FIELD_TIME_MSC = "time_msc";
    public const string FIELD_BID = "bid";
    public const string FIELD_ASK = "ask";
    public const string FIELD_LAST = "last";
    public const string FIELD_VOLUME_REAL = "volume_real";
    public const string FIELD_FLAGS = "flags";

    public const string BOUGHT_MODEL = "bought";
    public const string SOLD_MODEL = "sold";

    public AsyncServerStreamingCall<RatesRangeReply> StreamRatesRangeAsync(
        string symbol,
        DateTime utcFromDate,
        DateTime utcToDate,
        Timeframe timeframe,
        int chunkSize,
        CancellationToken cancellationToken)
    {
        var channel = grpcChannelPool.Get();
        var client = CreateClient(channel);

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
            grpcChannelPool.Return(channel);
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
        var channel = grpcChannelPool.Get();
        var client = CreateClient(channel);

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
            grpcChannelPool.Return(channel);
        }
    }

    public async Task<RatesRangeReply> GetRatesRangeFromTicksAsync(
        string symbol,
        DateTime utcFromDate,
        DateTime utcToDate,
        TimeSpan timeframe,
        CancellationToken cancellationToken)
    {
        var channel = grpcChannelPool.Get();
        var client = CreateClient(channel);

        try
        {
            var request = new GetRatesRangeFromTicksRequest
            {
                Symbol = symbol,
                FromDate = Timestamp.FromDateTime(utcFromDate),
                ToDate = Timestamp.FromDateTime(utcToDate),
                Timeframe = Duration.FromTimeSpan(timeframe),
            };

            return await client.GetRatesRangeFromTicksAsync(request, cancellationToken: cancellationToken);
        }
        finally
        {
            grpcChannelPool.Return(channel);
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
        var channel = grpcChannelPool.Get();
        var client = CreateClient(channel);

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
            grpcChannelPool.Return(channel);
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
        var channel = grpcChannelPool.Get();
        var client = CreateClient(channel);

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
            grpcChannelPool.Return(channel);
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
        var channel = grpcChannelPool.Get();
        var client = CreateClient(channel);

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
            grpcChannelPool.Return(channel);
        }
    }

    public async Task<GetSymbolTickReply> GetSymbolTickAsync(string symbol, CancellationToken cancellationToken)
    {
        var channel = grpcChannelPool.Get();
        var client = CreateClient(channel);

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
            grpcChannelPool.Return(channel);
        }
    }

    public async Task<PredictReply> PredictAsync(string modelName, double currentChange, CancellationToken cancellationToken)
    {
        var channel = grpcChannelPool.Get();
        var client = CreateClient(channel);

        try
        {
            var request = new PredictRequest
            {
                ModelName = modelName,
                CurrentChange = currentChange,
            };

            return await client.PredictAsync(request, cancellationToken: cancellationToken);
        }
        finally
        {
            grpcChannelPool.Return(channel);
        }
    }

    private static MarketData.MarketDataClient CreateClient(GrpcChannel channel)
    {
        return new MarketData.MarketDataClient(channel);
    }
}