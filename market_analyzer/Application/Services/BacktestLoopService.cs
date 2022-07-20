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
        private readonly List<Transaction> _transactions = new();

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

        private readonly decimal _rangePoints = 120;
        private readonly decimal _volume = 1;

        private readonly List<Range> _ranges = new();
        private int _rangesLastCount = 0;

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

            var endOfDay = quotes.Last().Date.TimeOfDay >= _end;

            if (_ranges.Count == _rangesLastCount && !endOfDay)
                return;

            var current = _positions.LastOrDefault(it => it.Volume() != 0);
            if (endOfDay && current is null)
                return;

            _rangesLastCount = _ranges.Count;

            if (_ranges.Count == 1)
                return;

            var tick = await GetTick(cancellationToken);

            var last = _ranges[^1];
            var previous = _ranges[^2];

            var isUp = (last.Value > previous.Value);
            var volume = isUp ? -_volume : _volume;

            if (current is not null && endOfDay)
                volume = current.Volume() * -1;

            var price = volume > 0 ? Convert.ToDecimal(tick.Trade.Ask) : Convert.ToDecimal(tick.Trade.Bid);
            var transaction = new Transaction(quotes.Last().Date, price, volume);

            if (current is null)
                _positions.Add(current = new Position());

            current.Add(transaction);

            PrintPosition(quotes, transaction);
        }

        private async Task<GetSymbolTickReply> GetTick(CancellationToken cancellationToken)
            => await _ratesProvider.GetSymbolTickAsync(_operationSettings.Value.MarketData.Symbol!, cancellationToken);

        private void PrintPosition(CustomQuote[] quotes, Transaction transaction)
        {
            if (_positions.Count == 0)
                return;

            var posProfit = 0M;
            var posVolume = 0M;

            foreach (var item in _positions)
            {
                posProfit += item.Profit(quotes.Last().Close);
                posVolume += item.Volume();
            }

            _logger.LogInformation("{@profit}", new
            {
                EntryTime = transaction.Time.ToString("yyyy-MM-ddTHH:mm:ss"),
                EntryPrice = transaction.Price,
                EntryVolume = transaction.Volume,
                Volume = posVolume,
                Profit = posProfit
            });
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
