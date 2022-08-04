using Application.Helpers;
using Application.Options;
using Application.Services.Providers.Cycle;
using Application.Services.Providers.Rates;
using Application.Services.Strategies;
using Grpc.Terminal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services
{
    public class BacktestLoopService : ILoopService
    {
        private readonly IRatesProvider _ratesProvider;
        private readonly BacktestCycleProvider _cycleProvider;
        private readonly IStrategyFactory _strategyFactory;
        private readonly IOptions<OperationSettings> _operationSettings;
        private readonly ILogger<ILoopService> _logger;

        private readonly Backtest _backtest = new();

        private bool _summaryPrinted = false;
        private DateTime _lastQuoteDate;

        public BacktestLoopService(
            IRatesProvider ratesProvider,
            BacktestCycleProvider cycleProvider,
            IStrategyFactory strategyFactory,
            IOptions<OperationSettings> operationSettings,
            ILogger<ILoopService> logger)
        {
            _ratesProvider = ratesProvider;
            _cycleProvider = cycleProvider;
            _strategyFactory = strategyFactory;
            _operationSettings = operationSettings;
            _logger = logger;
        }

        public bool IsEndOfDay =>
            _cycleProvider.Previous.TimeOfDay >= 
            _operationSettings.Value.End.ToTimeSpan().Subtract(_operationSettings.Value.Timeframe);

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            await _ratesProvider.CheckNewRatesAsync(
                _operationSettings.Value.Symbol.Name!,
                _operationSettings.Value.Date,
                _operationSettings.Value.Timeframe,
                _operationSettings.Value.StreamingData.ChunkSize,
                cancellationToken);

            var quotes = (await GetRatesAsync(cancellationToken)).ToQuotes().ToArray();

            var bookPrice = await GetBookPriceAsync(cancellationToken);
            var current = _backtest.OpenPosition();
            var balance = _backtest.Balance(bookPrice);

            var settings = _operationSettings.Value.Strategy;
            var strategy = _strategyFactory.Get(settings.Use) ?? throw new InvalidOperationException();

            var endOfDay = IsEndOfDay;

            if (settings.Profit is not null && balance.Profit >= settings.Profit)
                endOfDay = true;

            if (current is null && endOfDay)
            {
                if (!_summaryPrinted)
                {
                    _summaryPrinted = true;
                    _logger.LogInformation("{@summary}", _backtest.Summary);
                }

                if (settings.Profit is not null && balance.Profit >= settings.Profit)
                    throw new BacktestFinishException();

                return;
            }

            if (quotes.Length < strategy.LookbackPeriods + 1)
                return;

            if (!ChangeQuote(quotes))
                return;

            var beforeVolume = current is null ? 0 : current.Volume();

            if (strategy is IStrategy.IWithPosition s)
                if (current is not null)
                    s.Position = new StrategyPosition(current.Price(), beforeVolume, current.Profit(bookPrice));
                else
                    s.Position = null;

            var volume = strategy.SignalVolume(quotes);

            if (current is not null && endOfDay)
                volume = beforeVolume * -1;

            if (volume == 0)
                return;

            var transaction = _backtest.Execute(bookPrice, volume);

            Print(bookPrice, transaction);
        }

        private bool ChangeQuote(CustomQuote[] quotes)
        {
            var lastQuote = quotes[^1];
            if (_lastQuoteDate == lastQuote.Date)
                return false;
            _lastQuoteDate = lastQuote.Date;
            return true;
        }

        private async Task<BookPrice> GetBookPriceAsync(CancellationToken cancellationToken)
        {
            var tick = await _ratesProvider.GetSymbolTickAsync(_operationSettings.Value.Symbol.Name!, cancellationToken);
            return new BookPrice(tick.Trade.Time.ToDateTime(), tick.Trade.Bid!.Value, tick.Trade.Ask!.Value);
        }

        private async Task<IEnumerable<Rate>> GetRatesAsync(CancellationToken cancellationToken)
            => await _ratesProvider.GetRatesAsync(
                _operationSettings.Value.Symbol.Name!,
                _operationSettings.Value.Date,
                _operationSettings.Value.Timeframe,
                _operationSettings.Value.Window,
                cancellationToken);

        private void Print(BookPrice bookPrice, Transaction transaction)
        {
            if (!_backtest.Positions.Any())
                return;

            var result = _backtest.Balance(bookPrice);
            var symbol = _operationSettings.Value.Symbol;

            _logger.LogInformation("{@profit}", new
            {
                Time = transaction.Time.ToString("yyyy-MM-ddTHH:mm:ss.fff"),
                Volume = Math.Round(transaction.Volume, symbol.VolumeDecimals),
                transaction.Price,
                OpenVolume = Math.Round(result.OpenVolume, symbol.VolumeDecimals),
                OpenPrice = Math.Round(result.OpenPrice, symbol.PriceDecimals),
                Profit = Math.Round(result.Profit, symbol.PriceDecimals),
            });
        }
    }
}
