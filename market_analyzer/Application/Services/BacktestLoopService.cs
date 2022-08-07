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
            var settings = _operationSettings.Value.Strategy;
            var operationRisk = settings.OperationRisk;
            var dailyRisk = settings.DailyRisk;

            await _ratesProvider.CheckNewRatesAsync(
                _operationSettings.Value.Symbol.Name!,
                _operationSettings.Value.Date,
                _operationSettings.Value.Timeframe,
                _operationSettings.Value.StreamingData.ChunkSize,
                cancellationToken);

            var quotes = (await GetRatesAsync(cancellationToken)).ToQuotes().ToArray();

            var position = _backtest.OpenPosition();
            var bookPrice = await GetBookPriceAsync(cancellationToken);

            var positionProfit = position is not null ? position.Profit(bookPrice) : 0d;
            var dailyProfit = _backtest.Balance(bookPrice);

            var closeTheDay = HitRisk(dailyRisk, dailyProfit.Profit);
            var closeOperation = closeTheDay || HitRisk(operationRisk, positionProfit);

            if (position is null && (IsEndOfDay || closeTheDay))
            {
                _logger.LogInformation("{@summary}", _backtest.Summary);

                throw new BacktestFinishException();
            }

            var strategy = _strategyFactory.Get(settings.Use) ?? throw new InvalidOperationException();
            if (quotes.Length < strategy.LookbackPeriods + 1)
                return;

            if (!ChangeQuote(quotes) && !closeOperation)
                return;

            var beforeVolume = position is null ? 0 : position.Volume();

            if (strategy is IStrategy.IWithPosition s)
                if (position is not null)
                    s.Position = new StrategyPosition(position.Price(), beforeVolume, position.Profit(bookPrice));
                else
                    s.Position = null;

            double volume;

            if (position is not null && closeOperation)
                volume = beforeVolume * -1;
            else
                volume = strategy.SignalVolume(quotes);

            if (volume == 0)
                return;

            var afterVolume = beforeVolume + volume;

            if (afterVolume == 0 && !closeOperation)
                if (volume > 0)
                    volume = beforeVolume * -1 + settings.Volume;
                else
                    volume = beforeVolume * -1 + -settings.Volume;

            var transaction = _backtest.Execute(bookPrice, volume);

            Print(bookPrice, transaction);
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
