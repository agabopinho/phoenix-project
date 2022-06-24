using Google.Protobuf.WellKnownTypes;
using Grpc.Net.Client;
using Grpc.Terminal;

namespace Infrastructure.Platform
{
    public class MarketDataWrapper
    {
        public async Task CopyTicksRangeAsync(CancellationToken cancellationToken)
        {
            var channel = GrpcChannel.ForAddress("http://localhost:5051");

            var client = new MarketData.MarketDataClient(channel);

            var response = await client.CopyTicksRangeAsync(
                new CopyTicksRangeRequest
                {
                    Symbol = "PETR4",
                    FromDate = Timestamp.FromDateTime(new DateTime(2022, 6, 24, 12, 0, 0, DateTimeKind.Utc)),
                    ToDate = Timestamp.FromDateTime(new DateTime(2022, 6, 24, 12, 2, 0, DateTimeKind.Utc)),
                    Type = CopyTicks.Trade
                }, cancellationToken: cancellationToken);
        }
    }
}