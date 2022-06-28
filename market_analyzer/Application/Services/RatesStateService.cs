using Application.Helpers;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Terminal;
using Infrastructure.Terminal;
using StackExchange.Redis;

namespace Application.Services
{
    public interface IRatesStateService
    {
        Task CheckNewRatesAsync(
            string symbol, DateOnly date, TimeSpan timeframe,
            int chunkSize, CancellationToken cancellationToken);

        Task<IEnumerable<Rate>> GetRatesAsync(
            string symbol, DateOnly date, TimeSpan timeframe,
            TimeSpan window, CancellationToken cancellationToken);
    }

    public class RatesStateService : IRatesStateService
    {
        private readonly IMarketDataWrapper _marketDataWrapper;
        private readonly IDatabase _database;

        public RatesStateService(IMarketDataWrapper marketDataWrapper, IDatabase database)
        {
            _marketDataWrapper = marketDataWrapper;
            _database = database;
        }

        public async Task CheckNewRatesAsync(
            string symbol, DateOnly date, TimeSpan timeframe,
            int chunkSize, CancellationToken cancellationToken)
        {
            var ratesKey = RatesKey(symbol, date, $"{timeframe.TotalSeconds}s");
            var lastRate = await GetLastRateAsync(ratesKey);

            var fromDate = lastRate is null ?
                date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) :
                lastRate.Time.ToDateTime();

            var call = _marketDataWrapper.CopyRatesFromTicksRangeStream(
                symbol,
                fromDate,
                date.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc),
                timeframe,
                chunkSize,
                cancellationToken);

            var removeLastRate = lastRate;

            await foreach (var reply in call.ResponseStream.ReadAllAsync(cancellationToken: cancellationToken))
            {
                if (removeLastRate is not null)
                {
                    var score = Score(removeLastRate);
                    await _database.SortedSetRemoveRangeByScoreAsync(ratesKey, score, score);

                    removeLastRate = null;
                }

                var sets = reply.Rates
                    .Select(it =>
                        new SortedSetEntry(it.ToByteArray(), Score(it))
                    ).ToArray();

                if (sets.Any())
                    await _database.SortedSetAddAsync(ratesKey, sets);
            }
        }

        public async Task<IEnumerable<Rate>> GetRatesAsync(
            string symbol, DateOnly date, TimeSpan timeframe,
            TimeSpan window, CancellationToken cancellationToken)
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

        private async Task<Rate?> GetLastRateAsync(string ratesKey)
        {
            var rates = await _database.SortedSetRangeByRankAsync(ratesKey, stop: 0, order: StackExchange.Redis.Order.Descending);

            if (!rates.Any())
                return null;

            return Rate.Parser.ParseFrom(rates.First()!);
        }

        private static double Score(Rate rate)
            => rate.Time.ToDateTime().ToTimestamp();

        private static string RatesKey(string symbol, DateOnly date, string timeframe)
            => $"{symbol.ToLower()}:{date:yyyyMMdd}:rates:{timeframe}";
    }
}
