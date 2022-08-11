using Application.Helpers;
using Application.Options;
using Application.Services.Providers.Cycle;
using Application.Services.Providers.Rates;
using Application.Services.Strategies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;

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
        private DateTime _lastQuoteDate;
        private DateTime _nextTriggedTime;

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

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            _cycleProvider.TakeStep();

            var settings = _operationSettings.Value.Strategy;
            var operationRisk = settings.OperationRisk;
            var dailyRisk = settings.DailyRisk;

            await _ratesProvider.CheckNewRatesAsync(
                _operationSettings.Value.Symbol.Name!,
                _operationSettings.Value.Strategy.Date,
                _operationSettings.Value.Strategy.Timeframe,
                _operationSettings.Value.StreamingData.ChunkSize,
                cancellationToken);

            var quotes = await GetQuotesAsync(cancellationToken);

            var position = _backtest.OpenPosition();
            var bookPrice = await GetBookPriceAsync(cancellationToken);

            var positionProfit = position is not null ? position.Profit(bookPrice) : 0;
            var dailyProfit = _backtest.Balance(bookPrice);

            var hitDailyRisk = HitRisk(dailyRisk, dailyProfit.Profit);
            var closeOperation = hitDailyRisk || _cycleProvider.EndOfDay || HitRisk(operationRisk, positionProfit);

            if (position is null && (_cycleProvider.EndOfDay || hitDailyRisk))
                ThrowBacktestFinishException();

            var strategy = _strategyFactory.Get(settings.Use) ??
                throw new InvalidOperationException();

            if (!(closeOperation || Continue(quotes, strategy)))
                return;

            var beforeVolume = position is null ? 0 : position.Volume();

            SetStrategyPosition(position?.Price() ?? 0d, beforeVolume, positionProfit);

            double volume;

            if (position is not null && closeOperation)
                volume = beforeVolume * -1;
            else
                volume = strategy.SignalVolume(quotes);

            if (volume == 0)
                return;

            var transaction = _backtest.Execute(bookPrice, volume);

            Print(bookPrice, transaction);
        }

        private void ThrowBacktestFinishException()
        {
            _logger.LogInformation("{@summary}", _backtest.Summary);

            throw new BacktestFinishException();
        }

        private bool Continue(IEnumerable<IQuote> quotes, IStrategy strategy)
        {
            if (quotes.Count() < strategy.LookbackPeriods + 1)
                return false;

            if (!HasChanged(quotes))
                return false;

            if (!TriggedPeriodicTimer())
                return false;

            return true;
        }

        private void SetStrategyPosition(double positionPrice, double positionVolume, double positionProfit)
        {
            var strategies = _strategyFactory.GetAll();
            var position = new StrategyPosition(positionPrice, positionVolume, positionProfit);

            foreach (var strategy in strategies)
            {
                if (strategy is not IStrategy.IWithPosition s)
                    continue;

                s.Position = position;
            }
        }

        private static bool HitRisk(StrategySettings.Risk risk, double profit)
        {
            var closeOperation = false;

            if (risk.TakeProfit is not null && profit >= risk.TakeProfit)
                closeOperation = true;

            if (risk.StopLoss is not null && profit <= -risk.StopLoss)
                closeOperation = true;

            return closeOperation;
        }

        private bool HasChanged(IEnumerable<IQuote> quotes)
        {
            if (!_operationSettings.Value.Strategy.FireOnlyAtCandleOpening)
                return true;

            var lastQuote = quotes.Last();

            if (_lastQuoteDate == lastQuote.Date)
                return false;

            _lastQuoteDate = lastQuote.Date;

            return true;
        }

        private bool TriggedPeriodicTimer()
        {
            var periodictTimer = _operationSettings.Value.Strategy.PeriodicTimer;

            if (periodictTimer is null)
                return true;

            if (_nextTriggedTime == DateTime.MinValue)
                _nextTriggedTime = _cycleProvider.Start;

            if (_cycleProvider.Now() < _nextTriggedTime)
                return false;

            while (_nextTriggedTime <= _cycleProvider.Now())
                _nextTriggedTime += periodictTimer.Value;

            return true;
        }

        private async Task<BookPrice> GetBookPriceAsync(CancellationToken cancellationToken)
        {
            var tick = await _ratesProvider.GetSymbolTickAsync(_operationSettings.Value.Symbol.Name!, cancellationToken);

            return new BookPrice(
                Time: _cycleProvider.Now(),
                Bid: tick.Trade.Bid!.Value,
                Ask: tick.Trade.Ask!.Value);
        }

        private async Task<IEnumerable<IQuote>> GetQuotesAsync(CancellationToken cancellationToken)
        {
            var rates = await _ratesProvider.GetRatesAsync(
                _operationSettings.Value.Symbol.Name!,
                _operationSettings.Value.Strategy.Date,
                _operationSettings.Value.Strategy.Timeframe,
                cancellationToken);

            return rates.ToQuotes().ToArray();
        }

        private void Print(BookPrice bookPrice, Transaction transaction)
        {
            if (!_backtest.Positions.Any())
                return;

            var position = _backtest.OpenPosition();
            var result = _backtest.Balance(bookPrice);
            var symbol = _operationSettings.Value.Symbol;

            _logger.LogInformation("{@profit}", new
            {
                Time = transaction.Time.ToString("yyyy-MM-ddTHH:mm:ss.fff"),
                transaction.Price,
                Volume = Math.Round(transaction.Volume, symbol.VolumeDecimals),
                Open = new
                {
                    Volume = Math.Round(result.OpenVolume, symbol.VolumeDecimals),
                    Price = Math.Round(position?.Price() ?? 0d, symbol.PriceDecimals),
                    Profit = Math.Round(position?.Profit(bookPrice) ?? 0d, symbol.PriceDecimals),
                },
                DailyProfit = Math.Round(result.Profit, symbol.PriceDecimals),
            });
        }
    }
}
