using Application.Services.Providers.Cycle;
using Application.Services.Providers.Rates.BacktestRates;
using Google.Protobuf.WellKnownTypes;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Services.Providers.Rates
{
    public class BacktestRatesProvider : IRatesProvider
    {
        private readonly IBacktestRatesRepository _backtestRatesRepository;
        private readonly ICycleProvider _cycleProvider;
        private readonly ILogger<BacktestRatesProvider> _logger;
        private readonly Stopwatch _stopwatch;

        public BacktestRatesProvider(
            IBacktestRatesRepository backtestRatesRepository,
            ICycleProvider cycleProvider,
            ILogger<BacktestRatesProvider> logger)
        {
            _backtestRatesRepository = backtestRatesRepository;
            _cycleProvider = cycleProvider;
            _logger = logger;
            _stopwatch = new Stopwatch();
        }

        public bool Started { get; set; }
        public DateTime CurrentTime { get; private set; }
        public SortedList<DateTime, List<Trade>> Ticks { get; } = new();
        public SortedList<DateTime, Rate> Rates { get; } = new();

        public async Task CheckNewRatesAsync(
            string symbol,
            DateOnly date,
            TimeSpan timeframe,
            int chunkSize,
            CancellationToken cancellationToken)
        {
            CurrentTime = _cycleProvider.PlatformNow();

            if (Started)
                return;

            _logger.LogInformation("Loading backtest data {@data}", new { symbol, Date = date.ToShortDateString() });

            await LoadFromRepositoryAsync(symbol, date, cancellationToken);

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

            var now = CurrentTime;

            var fromDate = Rates.Any() ? Rates.Keys.Last() : now.Subtract(window);
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

                Rates[rate.Time.ToDateTime()] = rate;
            }

            foreach (var key in Rates.Keys.Where(key => key < now.Subtract(window)).ToArray())
                Rates.Remove(key);

            _logger.LogTrace("Create rates {@data}ms, count {@count}", _stopwatch.Elapsed.TotalMilliseconds, Rates.Count);
            _stopwatch.Restart();

            return Task.FromResult<IEnumerable<Rate>>(Rates.Values);
        }

        public Task<GetSymbolTickReply> GetSymbolTickAsync(string symbol, CancellationToken cancellationToken)
        {
            var now = CurrentTime;
            var toPartitionKey = PartitionKey(now);
            var index = Ticks.Keys.ToList().BinarySearch(toPartitionKey);

            if (index < 0)
                index = ~index - 1;

            var toTrade = new Trade { Time = now.ToTimestamp() };
            var tradeOnlyTimeComparer = new TradeOnlyTimeComparer();
            var trade = new Trade { Flags = 0 };

            for (int i = index; i > 0; i--)
            {
                var window = Ticks.Values[i];
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
            var fromPartitionKey = PartitionKey(fromDate);
            var toPartitionKey = PartitionKey(toDate);

            var index = Ticks.Keys.ToList().BinarySearch(fromPartitionKey);

            if (index < 0)
                index = ~index;

            var windowData = new List<Trade>();

            for (var i = index; i < Ticks.Keys.Count; i++)
            {
                var key = Ticks.Keys[i];

                if (key > toPartitionKey)
                    break;

                windowData.AddRange(Ticks.Values[i]);
            }

            return windowData.OrderBy(it => it.Time).ToList();
        }

        private async Task LoadFromRepositoryAsync(string symbol, DateOnly date, CancellationToken cancellationToken)
        {
            var ticks = await _backtestRatesRepository.GetTicksAsync(symbol, date, cancellationToken);

            foreach (var value in ticks)
            {
                var trade = new Trade
                {
                    Ask = value.Ask,
                    Bid = value.Bid,
                    Flags = (TickFlags)value.Flags,
                    Last = value.Last,
                    Time = Timestamp.FromDateTime(DateTime.SpecifyKind(value.Time, DateTimeKind.Utc)),
                    Volume = value.Volume,
                    VolumeReal = value.VolumeReal
                };

                var partitionKey = PartitionKey(trade.Time.ToDateTime());

                if (!Ticks.ContainsKey(partitionKey))
                    Ticks[partitionKey] = new();

                Ticks[partitionKey].Add(trade);
            }
        }

        private static SortedDictionary<DateTime, List<Trade>> ResampleData(DateTime fromDate, DateTime toDate, List<Trade> data, TimeSpan timeframe)
        {
            var indexes = GetIndexes(timeframe, fromDate, toDate);
            var resample = new SortedDictionary<DateTime, List<Trade>>();

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

        private static DateTime PartitionKey(DateTime time)
            => new(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, DateTimeKind.Utc);

        private static string TicksKey(string symbol, DateOnly date)
            => $"{symbol.ToLower()}:ticks:backtest:{date:yyyyMMdd}";

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
