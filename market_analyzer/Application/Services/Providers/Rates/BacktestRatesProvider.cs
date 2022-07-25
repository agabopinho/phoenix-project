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
        public SortedList<DateTime, List<TickData>> Ticks { get; } = new();
        public SortedList<DateTime, Rate> Rates { get; } = new();
        public List<DateTime> Keys { get; private set; } = new();

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

            var windowData = GetWindow(fromDate, toDate);

            _logger.LogTrace("Create window data {@data}ms, count {@count}", _stopwatch.Elapsed.TotalMilliseconds, windowData.Count);
            _stopwatch.Restart();

            var resample = ResampleData(fromDate, toDate, windowData, timeframe);

            _logger.LogTrace("Resample data {@data}ms, count {@count}", _stopwatch.Elapsed.TotalMilliseconds, resample.Count);
            _stopwatch.Restart();

            foreach (var rate in resample.Select(CreateRatesSelector))
            {
                if (rate is null)
                    continue;

                Rates[rate.Time.ToDateTime()] = rate;
            }

            _logger.LogTrace("Create rates {@data}ms, count {@count}", _stopwatch.Elapsed.TotalMilliseconds, Rates.Count);
            _stopwatch.Restart();

            return Task.FromResult<IEnumerable<Rate>>(Rates.Values);
        }

        public Task<GetSymbolTickReply> GetSymbolTickAsync(string symbol, CancellationToken cancellationToken)
        {
            var now = CurrentTime;
            var toPartitionKey = PartitionKey(now);
            var index = Keys.BinarySearch(toPartitionKey);

            if (index < 0)
                index = ~index - 1;

            var toTrade = new TickData { Time = now };
            var onlyTimeComparer = new TickDataOnlyTimeComparer();
            var trade = new TickData { Flags = 0 };

            for (int i = index; i > 0; i--)
            {
                var window = Ticks.Values[i];
                var end = window.BinarySearch(toTrade, onlyTimeComparer);

                if (end < 0)
                    end = ~end - 1;

                for (var t = end; t > 0; t--)
                {
                    var tick = window[t];

                    if (trade.Time == DateTime.MinValue)
                        trade.Time = tick.Time;

                    if (tick.Bid > 0 && trade.Bid <= 0)
                    {
                        trade.Bid = tick.Bid;
                        trade.Flags |= (int)TickFlags.Bid;
                    }

                    if (tick.Ask > 0 && trade.Ask <= 0)
                    {
                        trade.Ask = tick.Ask;
                        trade.Flags |= (int)TickFlags.Ask;
                    }

                    if (tick.Last > 0 && trade.Last <= 0)
                    {
                        trade.Last = tick.Last;
                        trade.Flags |= (int)TickFlags.Last;
                    }

                    if (TradeHasBidAskLast(trade))
                        break;
                }

                if (TradeHasBidAskLast(trade))
                    break;
            }

            return Task.FromResult(new GetSymbolTickReply
            {
                ResponseStatus = new ResponseStatus { ResponseCode = Res.SOk },
                Trade = new Trade
                {
                    Ask = trade.Ask,
                    Bid = trade.Bid,
                    Flags = (TickFlags)trade.Flags,
                    Last = trade.Last,
                    Time = trade.Time.ToTimestamp(),
                    Volume = trade.Volume,
                    VolumeReal = trade.VolumeReal,
                }
            });
        }

        private async Task LoadFromRepositoryAsync(string symbol, DateOnly date, CancellationToken cancellationToken)
        {
            var ticks = await _backtestRatesRepository.GetTicksAsync(symbol, date, cancellationToken);

            foreach (var trade in ticks)
            {
                trade.Time = DateTime.SpecifyKind(trade.Time, DateTimeKind.Utc);

                var partitionKey = PartitionKey(trade.Time);

                if (!Ticks.ContainsKey(partitionKey))
                    Ticks[partitionKey] = new();

                Ticks[partitionKey].Add(trade);
            }

            Keys = Ticks.Keys.ToList();
        }

        private List<List<TickData>> GetWindow(DateTime fromDate, DateTime toDate)
        {
            var fromPartitionKey = PartitionKey(fromDate);
            var toPartitionKey = PartitionKey(toDate);

            var startFrom = Keys.BinarySearch(fromPartitionKey);

            if (startFrom < 0)
                startFrom = ~startFrom;

            var windowData = new List<List<TickData>>();

            for (var i = startFrom; i < Ticks.Keys.Count; i++)
            {
                var key = Ticks.Keys[i];

                if (key > toPartitionKey)
                    break;

                windowData.Add(Ticks.Values[i]);
            }

            return windowData;
        }

        private static SortedList<DateTime, List<TickData>> ResampleData(DateTime fromDate, DateTime toDate, List<List<TickData>> windowData, TimeSpan timeframe)
        {
            var indexes = GetIndexes(timeframe, fromDate, toDate);
            var resample = new SortedList<DateTime, List<TickData>>();

            var fromTrade = new TickData { Time = fromDate };
            var onlyTimeComparer = new TickDataOnlyTimeComparer();

            var data = windowData.SelectMany(it => it).ToList();
            var startFrom = data.BinarySearch(fromTrade, onlyTimeComparer);

            if (startFrom < 0)
                startFrom = ~startFrom;

            for (var i = startFrom; i < data.Count; i++)
            {
                var trade = data[i];

                if (trade.Time > toDate)
                    break;

                int indexesIndex = indexes.BinarySearch(trade.Time);

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

        private static Rate? CreateRatesSelector(KeyValuePair<DateTime, List<TickData>> sample)
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

        private static bool TradeHasBidAskLast(TickData trade)
            => (trade.Flags & (int)TickFlags.Bid) == (int)TickFlags.Bid &&
                (trade.Flags & (int)TickFlags.Ask) == (int)TickFlags.Ask &&
                (trade.Flags & (int)TickFlags.Last) == (int)TickFlags.Last;

        private static DateTime PartitionKey(DateTime time)
            => new(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, DateTimeKind.Utc);

        private class TickDataOnlyTimeComparer : IComparer<TickData>
        {
            public int Compare(TickData? x, TickData? y)
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
