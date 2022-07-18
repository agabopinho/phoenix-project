using Application.Helpers;
using Application.Options;
using Application.Services.Providers.Rates;
using Grpc.Terminal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services
{
    public record class Transaction(DateTime Time, decimal Price, decimal Volume);

    public class Position
    {
        private List<Transaction> _transactions = new();

        public IEnumerable<Transaction> Transactions => _transactions.ToArray();

        public void Add(Transaction transaction)
            => _transactions.Add(transaction);

        public decimal Volume()
            => _transactions.Sum(it => it.Volume);

        public decimal Profit()
            => _transactions.Sum(it => it.Price * it.Volume) * -1;

        public decimal Profit(decimal marketPrice)
        {
            var open = _transactions.Sum(it => it.Price * it.Volume);
            var close = marketPrice * Volume() * -1;

            return (open + close) * -1;
        }
    }

    public record class Range(decimal Value, CustomQuote Quote);

    public class BacktestLoopService : ILoopService
    {
        private readonly IRatesProvider _ratesProvider;
        private readonly IOptionsSnapshot<OperationSettings> _operationSettings;
        private readonly ILogger<ILoopService> _logger;
        private readonly TimeSpan _end;

        private TimeSpan? _entryEvery = null;
        private readonly decimal _rangePoints = 200;
        private readonly Dictionary<decimal, decimal> _volume = new() { { 0M, 1M } };

        private readonly List<Range> _ranges = new();
        private int _rangesLastCount = 0;
        private TimeSpan _lastEntry = TimeSpan.Zero;

        private readonly List<Position> _positions = new();

        public BacktestLoopService(
            IRatesProvider ratesProvider,
            IOptionsSnapshot<OperationSettings> operationSettings,
            ILogger<ILoopService> logger)
        {
            _ratesProvider = ratesProvider;
            _operationSettings = operationSettings;
            _logger = logger;
            _end = _operationSettings.Value.End.ToTimeSpan().Subtract(TimeSpan.FromMinutes(1));
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var marketData = _operationSettings.Value.MarketData;

            await _ratesProvider.CheckNewRatesAsync(
                marketData.Symbol!,
                marketData.Date,
                marketData.Timeframe,
                marketData.ChunkSize,
                cancellationToken);

            await CheckAsync(cancellationToken);
        }

        private async Task CheckAsync(CancellationToken cancellationToken)
        {
            var quotes = (await GetRatesAsync(cancellationToken)).ToQuotes().ToArray();

            if (!quotes.Any())
                return;

            UpdateRange(quotes);

            var currentTime = (_ratesProvider as BacktestRatesProvider)!.CurrentTime;
            var runEver = _entryEvery is not null && currentTime.TimeOfDay - _lastEntry > _entryEvery;

            var endOfDay = quotes.Last().Date.TimeOfDay >= _end;

            if (_ranges.Count == _rangesLastCount && !endOfDay && !runEver)
                return;

            var current = _positions.LastOrDefault(it => it.Volume() != 0);
            if (endOfDay && current is null)
                return;

            _lastEntry = currentTime.TimeOfDay;
            _rangesLastCount = _ranges.Count;

            if (_ranges.Count == 1)
                return;

            var tick = await _ratesProvider.GetSymbolTickAsync(_operationSettings.Value.MarketData.Symbol!, cancellationToken);

            var last = _ranges[^1];
            var previous = _ranges[^2];

            var value = _volume.First();
            var isUp = last.Value > previous.Value;
            var volume = isUp ? -value.Value : value.Value; // up: sell, down: buy

            if (current is not null)
            {
                var currentVolume = current.Volume();

                if (endOfDay)
                    volume = currentVolume * -1; // close
                else if (currentVolume < 0 && volume > 0 || currentVolume > 0 && volume < 0)
                    volume += currentVolume * -1; // invert
                else
                {
                    // increment (avg price)
                    value = _volume.Last(it => it.Key <= Math.Abs(currentVolume));
                    volume = isUp ? -value.Value : value.Value;
                }
            }

            var price = volume > 0 ? Convert.ToDecimal(tick.Trade.Ask) : Convert.ToDecimal(tick.Trade.Bid);
            var transaction = new Transaction(quotes.Last().Date, price, volume);

            if (current is null)
                _positions.Add(current = new Position());

            current.Add(transaction);

            if (_positions.Count > 0)
            {
                var posProfit = 0M;
                var posVolume = 0M;

                foreach (var item in _positions)
                {
                    posProfit += item.Profit(quotes.Last().Close);
                    posVolume += item.Volume();
                }

                _logger.LogInformation("{@profit}", new
                {
                    transaction,
                    position = new { volume = posVolume, profit = posProfit }
                });
            }
        }

        private void UpdateRange(CustomQuote[] quotes)
        {
            if (!quotes.Any())
                return;

            if (!_ranges.Any())
                _ranges.Add(new(quotes.First().Open, quotes.First() with { }));

            var lastQuote = quotes.Last();

            while (lastQuote.Close >= _ranges.Last().Value + _rangePoints)
                _ranges.Add(new(_ranges.Last().Value + _rangePoints, lastQuote with { }));

            while (lastQuote.Close <= _ranges.Last().Value - _rangePoints)
                _ranges.Add(new(_ranges.Last().Value - _rangePoints, lastQuote with { }));
        }

        private async Task<IEnumerable<Rate>> GetRatesAsync(CancellationToken cancellationToken)
        {
            var marketData = _operationSettings.Value.MarketData;

            var result = await _ratesProvider.GetRatesAsync(
                marketData.Symbol!,
                marketData.Date,
                marketData.Timeframe,
                marketData.Window,
                cancellationToken);

            return result.OrderBy(it => it.Time).ToArray();
        }
    }
}
