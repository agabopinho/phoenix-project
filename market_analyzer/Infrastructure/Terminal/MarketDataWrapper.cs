using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Grpc.Net.ClientFactory;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Terminal
{
    public interface IMarketDataWrapper
    {
        AsyncServerStreamingCall<CopyRatesRangeReply> CopyRatesRangeStream(
            string symbol,
            DateTime utcFromDate,
            DateTime utcToDate,
            Timeframe timeframe,
            int chunkSize,
            CancellationToken cancellationToken);

        AsyncServerStreamingCall<CopyRatesRangeReply> CopyRatesFromTicksRangeStream(
            string symbol,
            DateTime utcFromDate,
            DateTime utcToDate,
            TimeSpan timeframe,
            int chunkSize,
            CancellationToken cancellationToken);

        AsyncServerStreamingCall<CopyTicksRangeReply> CopyTicksRangeStream(
            string symbol,
            DateTime utcFromDate,
            DateTime utcToDate,
            CopyTicks type,
            int chunkSize,
            CancellationToken cancellationToken);
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

        public AsyncServerStreamingCall<CopyTicksRangeReply> CopyTicksRangeStream(
            string symbol,
            DateTime utcFromDate,
            DateTime utcToDate,
            CopyTicks type,
            int chunkSize,
            CancellationToken cancellationToken)
        {
            var client = _grpcClientFactory.CreateClient<MarketData.MarketDataClient>(ClientName);

            var request = new CopyTicksRangeRequest
            {
                Symbol = symbol,
                FromDate = Timestamp.FromDateTime(utcFromDate),
                ToDate = Timestamp.FromDateTime(utcToDate),
                Type = type,
                ChunckSize = chunkSize
            };

            return client.CopyTicksRangeStream(request, cancellationToken: cancellationToken);
        }

        public AsyncServerStreamingCall<CopyRatesRangeReply> CopyRatesRangeStream(
           string symbol,
           DateTime utcFromDate,
           DateTime utcToDate,
           Timeframe timeframe,
           int chunkSize,
           CancellationToken cancellationToken)
        {
            var client = _grpcClientFactory.CreateClient<MarketData.MarketDataClient>(ClientName);

            var request = new CopyRatesRangeRequest
            {
                Symbol = symbol,
                FromDate = Timestamp.FromDateTime(utcFromDate),
                ToDate = Timestamp.FromDateTime(utcToDate),
                Timeframe = timeframe,
                ChunckSize = chunkSize
            };

            return client.CopyRatesRangeStream(request, cancellationToken: cancellationToken);
        }

        public AsyncServerStreamingCall<CopyRatesRangeReply> CopyRatesFromTicksRangeStream(
           string symbol,
           DateTime utcFromDate,
           DateTime utcToDate,
           TimeSpan timeframe,
           int chunkSize,
           CancellationToken cancellationToken)
        {
            var client = _grpcClientFactory.CreateClient<MarketData.MarketDataClient>(ClientName);

            var request = new CopyRatesFromTicksRangeRequest
            {
                Symbol = symbol,
                FromDate = Timestamp.FromDateTime(utcFromDate),
                ToDate = Timestamp.FromDateTime(utcToDate),
                Timeframe = Duration.FromTimeSpan(timeframe),
                ChunckSize = chunkSize
            };

            return client.CopyRatesFromTicksRangeStream(request, cancellationToken: cancellationToken);
        }
    }
}