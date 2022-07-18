using Application.Services.Providers.Cycle;
using Application.Services.Providers.Database;
using Google.Protobuf.WellKnownTypes;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Services.Providers.Rates
{
    public class BacktestRatesProvider : IRatesProvider
    {
        private readonly IBacktestDatabaseProvider _backtestDatabase;
        private readonly ICycleProvider _cycleProvider;
        private readonly ILogger<BacktestRatesProvider> _logger;
        private readonly Stopwatch _stopwatch;

        private DateTime _currentTime;
        private readonly SortedList<DateTime, Rate> _rates = new();

        public BacktestRatesProvider(
            IBacktestDatabaseProvider backtestDatabase,
            ICycleProvider cycleProvider,
            ILogger<BacktestRatesProvider> logger)
        {
            _backtestDatabase = backtestDatabase;
            _cycleProvider = cycleProvider;
            _logger = logger;
            _stopwatch = new Stopwatch();
        }

        public bool Started { get; set; }

        public async Task CheckNewRatesAsync(
            string symbol,
            DateOnly date,
            TimeSpan timeframe,
            int chunkSize,
            CancellationToken cancellationToken)
        {
            _currentTime = _cycleProvider.PlatformNow();

            if (Started)
                return;

            if (await _backtestDatabase.LoadAsync(symbol, date, chunkSize, cancellationToken))
                Started = true;
        }

        public Task<IEnumerable<Rate>> GetRatesAsync(
            string symbol,
            DateOnly date,
            TimeSpan timeframe,
            TimeSpan window,
            CancellationToken cancellationToken)
        {
            _stopwatch.Restart();

            var now = _currentTime;

            var fromDate = _rates.Any() ? _rates.Keys.Last() : now.Subtract(window);
            var toDate = now;

            var data = GetWindow(fromDate, toDate);

            _logger.LogTrace("Create window data {@data}ms, count {@count}", _stopwatch.Elapsed.TotalMilliseconds, data.Count);
            _stopwatch.Restart();

            var resample = ResampleData(fromDate, toDate, data, timeframe);

            _logger.LogTrace("Resample data {@data}ms, count {@count}", _stopwatch.Elapsed.TotalMilliseconds, resample.Count);
            _stopwatch.Restart();

            foreach (var rate in resample.Select(CreateRatesSelector))
            {
                if (rate is null)
                    continue;

                var rateTime = rate.Time.ToDateTime();
                _rates[rateTime] = rate;
            }

            foreach (var key in _rates.Keys.Where(key => key < now.Subtract(window)).ToArray())
                _rates.Remove(key);

            _logger.LogTrace("Create rates {@data}ms, count {@count}", _stopwatch.Elapsed.TotalMilliseconds, _rates.Count);
            _stopwatch.Restart();

            return Task.FromResult<IEnumerable<Rate>>(_rates.Values);
        }

        public Task<GetSymbolTickReply> GetSymbolTickAsync(string symbol, CancellationToken cancellationToken)
        {
            var now = _currentTime;
            var toPartitionKey = _backtestDatabase.PartitionKey(now);
            var index = _backtestDatabase.TicksDatabase.Keys.ToList().BinarySearch(toPartitionKey);

            if (index < 0)
                index = ~index - 1;

            var toTrade = new Trade { Time = now.ToTimestamp() };
            var tradeOnlyTimeComparer = new TradeOnlyTimeComparer();
            var trade = new Trade { Flags = 0 };

            for (int i = index; i > 0; i--)
            {
                var window = _backtestDatabase.TicksDatabase.Values[i];
                var end = window.BinarySearch(toTrade, tradeOnlyTimeComparer);

                if (end < 0)
                    end = ~end - 1;

                for (var t = end; t > 0; t--)
                {
                    var tick = window[t];

                    if (trade.Time is null)
                        trade.Time = tick.Time;

                    if (trade.Bid is null && tick.Bid > 0)
                    {
                        trade.Bid = tick.Bid;
                        trade.Flags |= TickFlags.Bid;
                    }

                    if (trade.Ask is null && tick.Ask > 0)
                    {
                        trade.Ask = tick.Ask;
                        trade.Flags |= TickFlags.Ask;
                    }

                    if (trade.Last is null && tick.Last > 0)
                    {
                        trade.Last = tick.Last;
                        trade.Flags |= TickFlags.Last;
                    }

                    if (TradeHasBidAskLast(trade))
                        break;
                }

                if (TradeHasBidAskLast(trade))
                    break;
            }

            var reply = new GetSymbolTickReply
            {
                ResponseStatus = new ResponseStatus { ResponseCode = Res.SOk },
                Trade = trade
            };

            return Task.FromResult(reply);
        }

        private List<Trade> GetWindow(DateTime fromDate, DateTime toDate)
        {
            var fromPartitionKey = _backtestDatabase.PartitionKey(fromDate);
            var toPartitionKey = _backtestDatabase.PartitionKey(toDate);

            var index = _backtestDatabase.TicksDatabase.Keys.ToList().BinarySearch(fromPartitionKey);

            if (index < 0)
                index = ~index;

            var windowData = new List<Trade>();

            for (var i = index; i < _backtestDatabase.TicksDatabase.Keys.Count; i++)
            {
                var key = _backtestDatabase.TicksDatabase.Keys[i];

                if (key > toPartitionKey)
                    break;

                windowData.AddRange(_backtestDatabase.TicksDatabase.Values[i]);
            }

            return windowData;
        }

        private static IDictionary<DateTime, List<Trade>> ResampleData(DateTime fromDate, DateTime toDate, List<Trade> data, TimeSpan timeframe)
        {
            var indexes = GetIndexes(timeframe, fromDate, toDate);
            var resample = new Dictionary<DateTime, List<Trade>>(indexes.Count);

            var fromDateTimestamp = fromDate.ToTimestamp();
            var toDateTimestamp = toDate.ToTimestamp();

            var fromTrade = new Trade { Time = fromDateTimestamp };
            var tradeOnlyTimeComparer = new TradeOnlyTimeComparer();

            var startFrom = data.BinarySearch(fromTrade, tradeOnlyTimeComparer);

            if (startFrom < 0)
                startFrom = ~startFrom;

            for (var i = startFrom; i < data.Count; i++)
            {
                var trade = data[i];

                if (trade.Time > toDateTimestamp)
                    break;

                int indexesIndex = indexes.BinarySearch(trade.Time.ToDateTime());

                if (indexesIndex < 0)
                    indexesIndex = ~indexesIndex - 1;

                var indexDate = indexes[indexesIndex];

                if (!resample.ContainsKey(indexDate))
                    resample[indexDate] = new();

                resample[indexDate].Add(trade);
            }

            return resample;
        }

        private static List<DateTime> GetIndexes(TimeSpan timeframe, DateTime min, DateTime max)
        {
            min = new DateTime(min.Year, min.Month, min.Day, min.Hour, min.Minute, 0, DateTimeKind.Utc);
            max = new DateTime(max.Year, max.Month, max.Day, max.Hour, max.Minute, 0, DateTimeKind.Utc).AddMinutes(1);

            var indexes = new List<DateTime>();

            for (DateTime d = min; d < max; d = d.Add(timeframe))
                indexes.Add(d);

            return indexes;
        }

        private static Rate? CreateRatesSelector(KeyValuePair<DateTime, List<Trade>> sample)
        {
            var data = sample.Value;
            var prices = sample.Value.Where(it => it.Last > 0).Select(it => it.Last);

            if (!prices.Any())
                return null;

            return new Rate
            {
                Time = Timestamp.FromDateTime(sample.Key),
                Open = prices.First(),
                High = prices.Max(),
                Low = prices.Min(),
                Close = prices.Last(),
                Spread = 0,
                TickVolume = prices.Count(),
                Volume = data.Sum(it => it.Volume)
            };
        }

        private static bool TradeHasBidAskLast(Trade trade)
            => (trade.Flags & TickFlags.Bid) == TickFlags.Bid &&
                (trade.Flags & TickFlags.Ask) == TickFlags.Ask &&
                (trade.Flags & TickFlags.Last) == TickFlags.Last;

        private class TradeOnlyTimeComparer : IComparer<Trade>
        {
            public int Compare(Trade? x, Trade? y)
            {
                if (x is null && y is null)
                    return 0;
                else if (x is not null && y is null)
                    return 1;
                else if (x is null && y is not null)
                    return -1;

                if (x!.Time > y!.Time)
                    return 1;
                else if (x.Time < y.Time)
                    return -1;
                else
                    return 0;
            }
        }
    }
}
