using Google.Protobuf;
using Grpc.Core;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Application.Services.Providers.Database
{
    public class BacktestDatabaseProvider : IBacktestDatabaseProvider
    {
        private readonly IMarketDataWrapper _marketDataWrapper;
        private readonly IDatabase _database;
        private readonly ILogger<BacktestDatabaseProvider> _logger;

        public BacktestDatabaseProvider(IMarketDataWrapper marketDataWrapper, IDatabase database, ILogger<BacktestDatabaseProvider> logger)
        {
            _marketDataWrapper = marketDataWrapper;
            _database = database;
            _logger = logger;
        }

        public SortedList<DateTime, List<Trade>> TicksDatabase { get; } = new();

        public async Task<bool> LoadAsync(string symbol, DateOnly date, int chunkSize, CancellationToken cancellationToken)
        {
            var key = BacktestTicksKey(symbol, date);

            await FromCacheAsync(key);

            var fromDate = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
            var toDate = date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

            if (TicksDatabase.Count > 0)
                fromDate = TicksDatabase
                    .SelectMany(it => it.Value)
                    .Select(it => it.Time.ToDateTime())
                    .Max();

            _logger.LogInformation("Loading backtest data {@data}", new { symbol, date });

            using var call = _marketDataWrapper.StreamTicksRange(symbol, fromDate, toDate, CopyTicks.All, chunkSize, cancellationToken);

            await foreach (var reply in call.ResponseStream.ReadAllAsync(cancellationToken: cancellationToken))
            {
                if (reply.ResponseStatus.ResponseCode != Res.SOk)
                    return false;

                var values = new List<RedisValue>(reply.Trades.Count);

                foreach (var trade in reply.Trades)
                {
                    var tradeTime = trade.Time.ToDateTime();

                    if (tradeTime <= fromDate)
                        continue;

                    var partitionKey = PartitionKey(tradeTime);

                    if (!TicksDatabase.ContainsKey(partitionKey))
                        TicksDatabase[partitionKey] = new();

                    TicksDatabase[partitionKey].Add(trade);

                    values.Add((RedisValue)trade.ToByteArray());
                }

                await _database.ListRightPushAsync(key, values.ToArray());

                _logger.LogInformation("Chunk {@data}", new { reply.Trades.Count, Total = TicksDatabase.SelectMany(it => it.Value).Count() });
            }

            return true;
        }

        public DateTime PartitionKey(DateTime time)
            => new(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, DateTimeKind.Utc);

        private async Task FromCacheAsync(string key)
        {
            var values = await _database.ListRangeAsync(key);

            foreach (var value in values)
            {
                var trade = Trade.Parser.ParseFrom((byte[])value!);
                var partitionKey = PartitionKey(trade.Time.ToDateTime());

                if (!TicksDatabase.ContainsKey(partitionKey))
                    TicksDatabase[partitionKey] = new();

                TicksDatabase[partitionKey].Add(trade);
            }
        }

        private static string BacktestTicksKey(string symbol, DateOnly date)
            => $"{symbol.ToLower()}:ticks:backtest:{date:yyyyMMdd}";
    }
}
