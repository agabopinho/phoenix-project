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
    }
}