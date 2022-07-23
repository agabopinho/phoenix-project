using Application.Helpers;
using Application.Options;
using Application.Services.Providers.Rates;
using Grpc.Terminal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services
{
    public class BacktestLoopService : ILoopService
    {
        private readonly IRatesProvider _ratesProvider;
        private readonly IOptionsSnapshot<OperationSettings> _operationSettings;
        private readonly ILogger<ILoopService> _logger;
        private readonly TimeSpan _end;

        private readonly List<Range> _ranges = new();
        private int _rangesLastCount = 0;
        public decimal _rangePoints;

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
            _rangePoints = _operationSettings.Value.Strategy.RangePoints;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var symbolData = _operationSettings.Value.Symbol;

            await _ratesProvider.CheckNewRatesAsync(
                symbolData.Name!,
                _operationSettings.Value.Date,
                _operationSettings.Value.Timeframe,
                _operationSettings.Value.StreamingData.ChunkSize,
                cancellationToken);

            await CheckAsync(cancellationToken);
        }

        private async Task CheckAsync(CancellationToken cancellationToken)
        {
            var quotes = (await GetRatesAsync(cancellationToken)).ToQuotes().ToArray();

            if (!quotes.Any())
                return;

            ComputeRange(quotes);

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

            var strategy = _operationSettings.Value.Strategy;
            var isUp = (last.Value > previous.Value);
            var volume = isUp ? -strategy.Volume : strategy.Volume;

            if (current is not null)
            {
                var symbol = _operationSettings.Value.Symbol;

                var moreVolume = Math.Abs(current.Volume()) * strategy.MoreVolumeFactor;
                var mod = moreVolume % symbol.StandardLot;

                if (volume > 0)
                    volume += moreVolume - mod;
                else
                    volume -= moreVolume - mod;

                if (Math.Abs(volume) > strategy.MaxMoreVolume)
                {
                    if (volume > 0)
                        volume = strategy.MaxMoreVolume;
                    else
                        volume = -strategy.MaxMoreVolume;
                }
            }

            ResizeRange(current is not null ? current.Volume() : volume);

            if (current is not null && current.Volume() + volume == 0)
                volume *= 2;

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
            => await _ratesProvider.GetSymbolTickAsync(_operationSettings.Value.Symbol.Name!, cancellationToken);

        private void PrintPosition(CustomQuote[] quotes, Transaction transaction)
        {
            if (_positions.Count == 0)
                return;

            var posProfit = 0M;
            var posVolume = 0M;
            var posPrice = 0M;

            foreach (var item in _positions)
            {
                posProfit += item.Profit(quotes.Last().Close);
                posVolume += item.Volume();
                posPrice = item.Price();
            }

            var symbol = _operationSettings.Value.Symbol;

            _logger.LogInformation("{@profit}", new
            {
                Time = transaction.Time.ToString("yyyy-MM-ddTHH:mm:ss"),
                transaction.Price,
                Volume = Math.Round(transaction.Volume, symbol.VolumeDecimals),
                PosPrice = Math.Round(posPrice, symbol.PriceDecimals),
                PosVolume = Math.Round(posVolume, symbol.VolumeDecimals),
                PosProfit = Math.Round(posProfit, symbol.PriceDecimals),
            });
        }

        private void ResizeRange(decimal volume)
        {
            var strategy = _operationSettings.Value.Strategy;
            var rangePoints = strategy.RangePoints;
            var p = strategy.MoreRangeFactor * Math.Abs(volume) / strategy.Volume / 100;
            if (p > strategy.MaxMoreRange) p = strategy.MaxMoreRange;
            _rangePoints = rangePoints + rangePoints * p;
        }

        private void ComputeRange(CustomQuote[] quotes)
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
            var symbolData = _operationSettings.Value.Symbol;

            var result = await _ratesProvider.GetRatesAsync(
                symbolData.Name!,
                _operationSettings.Value.Date,
                _operationSettings.Value.Timeframe,
                _operationSettings.Value.Window,
                cancellationToken);

            return result.OrderBy(it => it.Time).ToArray();
        }
    }
}
