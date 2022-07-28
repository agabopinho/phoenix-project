using Application.Helpers;
using Application.Options;
using Application.Services.Providers.Cycle;
using Application.Services.Providers.Rates;
using Grpc.Terminal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;

namespace Application.Services
{
    public class BacktestLoopService : ILoopService
    {
        private readonly IRatesProvider _ratesProvider;
        private readonly BacktestCycleProvider _cycleProvider;
        private readonly IOptions<OperationSettings> _operationSettings;
        private readonly ILogger<ILoopService> _logger;
        private readonly TimeSpan _end;

        private readonly Backtest _backtest = new();

        private bool _summaryPrinted = false;
        private DateTime _lastRateDate;

        public BacktestLoopService(
            IRatesProvider ratesProvider,
            BacktestCycleProvider cycleProvider,
            IOptions<OperationSettings> operationSettings,
            ILogger<ILoopService> logger)
        {
            _ratesProvider = ratesProvider;
            _cycleProvider = cycleProvider;
            _operationSettings = operationSettings;
            _logger = logger;
            _end = _operationSettings.Value.End.ToTimeSpan().Subtract(TimeSpan.FromMinutes(1));
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            await _ratesProvider.CheckNewRatesAsync(
                _operationSettings.Value.Symbol.Name!,
                _operationSettings.Value.Date,
                _operationSettings.Value.Timeframe,
                _operationSettings.Value.StreamingData.ChunkSize,
                cancellationToken);

            await StrategyAsync(cancellationToken);
        }

        private async Task StrategyAsync(CancellationToken cancellationToken)
        {
            var rates = (await GetRatesAsync(cancellationToken)).ToQuotes().ToArray();
            var tick = await GetTickAsync(cancellationToken);

            var isEndOfDay = _cycleProvider.Previous.TimeOfDay >= _end;

            var strategy = _operationSettings.Value.Strategy;
            var current = _backtest.OpenPosition();

            if (current is null && isEndOfDay)
            {
                if (!_summaryPrinted)
                {
                    _summaryPrinted = true;
                    _logger.LogInformation("{@summary}", _backtest.Summary);
                }

                return;
            }

            if (rates.Length < strategy.AtrLookbackPeriods + 1)
                return;

            var lastRate = rates[^1];
            if (_lastRateDate == lastRate.Date)
                return;
            _lastRateDate = lastRate.Date;

            var stopAtr = rates.GetVolatilityStop(strategy.AtrLookbackPeriods, strategy.AtrMultiplier);
            var lastStopAtr = stopAtr.Last();

            var volume = lastStopAtr.LowerBand is not null ? -strategy.Volume : strategy.Volume;

            if (current is not null)
            {
                var v = Math.Abs(current.BalanceVolume()) * strategy.IncrementVolume;

                v -= v % _operationSettings.Value.Symbol.StandardLot;

                if (volume > 0)
                    volume += v;
                else
                    volume -= v;
            }

            var beforeVolume = current is null ? 0 : current.BalanceVolume();
            var afterVolume = beforeVolume + volume;

            if (current is not null && afterVolume == 0)
                volume *= 2;

            if (current is not null && isEndOfDay)
                volume = current.BalanceVolume() * -1;

            if (volume == 0)
                return;

            var price = volume > 0 ? Convert.ToDecimal(tick.Trade.Ask) : Convert.ToDecimal(tick.Trade.Bid);
            var transaction = new Transaction(_cycleProvider.Previous, price, volume);

            _backtest.Add(transaction);

            PrintPosition(tick, transaction);
        }

        private async Task<IEnumerable<Rate>> GetRatesAsync(CancellationToken cancellationToken)
            => await _ratesProvider.GetRatesAsync(
                _operationSettings.Value.Symbol.Name!,
                _operationSettings.Value.Date,
                _operationSettings.Value.Timeframe,
                _operationSettings.Value.Window,
                cancellationToken);

        private async Task<GetSymbolTickReply> GetTickAsync(CancellationToken cancellationToken)
            => await _ratesProvider.GetSymbolTickAsync(_operationSettings.Value.Symbol.Name!, cancellationToken);

        private void PrintPosition(GetSymbolTickReply tick, Transaction transaction)
        {
            if (_backtest.Positions.Count() == 0)
                return;

            var bookPrice = new BookPrice(Convert.ToDecimal(tick.Trade.Bid), Convert.ToDecimal(tick.Trade.Ask));
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
