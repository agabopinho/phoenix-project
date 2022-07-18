using Application.Helpers;
using Application.Services.Providers.Cycle;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Terminal;
using Infrastructure.GrpcServerTerminal;
using StackExchange.Redis;

namespace Application.Services.Providers.Rates
{
    public class RedisOnlineRatesProvider : IRatesProvider
    {
        private readonly IMarketDataWrapper _marketDataWrapper;
        private readonly IDatabase _database;
        private readonly ICycleProvider _cycleProvider;

        public RedisOnlineRatesProvider(
            IMarketDataWrapper marketDataWrapper,
            IDatabase database,
            ICycleProvider cycleProvider)
        {
            _marketDataWrapper = marketDataWrapper;
            _database = database;
            _cycleProvider = cycleProvider;
        }

        public async Task CheckNewRatesAsync(
            string symbol,
            DateOnly date,
            TimeSpan timeframe,
            int chunkSize,
            CancellationToken cancellationToken)
        {
            var ratesKey = RatesKey(symbol, date, $"{timeframe.TotalSeconds}s");
            var lastRate = await GetLastRateAsync(ratesKey);

            var fromDate = lastRate is null ?
                date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) :
                lastRate.Time.ToDateTime();

            using var call = _marketDataWrapper.StreamRatesFromTicksRange(
                symbol,
                fromDate,
                _cycleProvider.PlatformNow().AddSeconds(10),
                timeframe,
                chunkSize,
                cancellationToken);

            await foreach (var reply in call.ResponseStream.ReadAllAsync(cancellationToken: cancellationToken))
            {
                if (!reply.Rates.Any())
                    continue;

                if (lastRate is not null)
                {
                    var removed = await _database.SortedSetRemoveAsync(ratesKey, lastRate.ToByteArray());
                    lastRate = null;
                }

                var sortedSetEntries = reply.Rates.Select(it => new SortedSetEntry(it.ToByteArray(), Score(it))).ToArray();

                await _database.SortedSetAddAsync(ratesKey, sortedSetEntries);
            }
        }

        public async Task<IEnumerable<Rate>> GetRatesAsync(
            string symbol,
            DateOnly date,
            TimeSpan timeframe,
            TimeSpan window,
            CancellationToken cancellationToken)
        {
            var ratesKey = RatesKey(symbol, date, $"{timeframe.TotalSeconds}s");
            var lastRate = await GetLastRateAsync(ratesKey);

            if (lastRate is null)
                return Array.Empty<Rate>();

            var lastRateScore = Score(lastRate);
            var rates = await _database.SortedSetRangeByScoreAsync(
                ratesKey,
                start: lastRateScore - window.TotalSeconds,
                stop: lastRateScore,
                order: StackExchange.Redis.Order.Descending);

            return rates.Select(it => Rate.Parser.ParseFrom(it));
        }

        public async Task<GetSymbolTickReply> GetSymbolTickAsync(string symbol, CancellationToken cancellationToken)
            => await _marketDataWrapper.GetSymbolTickAsync(symbol, cancellationToken);

        private async Task<Rate?> GetLastRateAsync(string ratesKey)
        {
            var rates = await _database.SortedSetRangeByRankAsync(ratesKey, stop: 0, order: StackExchange.Redis.Order.Descending);

            if (!rates.Any())
                return null;

            return Rate.Parser.ParseFrom(rates.First()!);
        }

        private static double Score(Rate rate)
            => rate.Time.ToDateTime().ToUnixEpochTimestamp();

        private static string RatesKey(string symbol, DateOnly date, string timeframe)
            => $"{symbol.ToLower()}:{date:yyyyMMdd}:rates:{timeframe}";
    }
}
