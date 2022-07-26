using Application.Helpers;
using Application.Options;
using Application.Services.Providers.Cycle;
using Application.Services.Providers.Rates;
using Grpc.Terminal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services
{
    public class BacktestLoopService : ILoopService
    {
        private readonly IRatesProvider _ratesProvider;
        private readonly BacktestCycleProvider _cycleProvider;
        private readonly IOptions<OperationSettings> _operationSettings;
        private readonly ILogger<ILoopService> _logger;
        private readonly TimeSpan _end;

        private readonly List<Position> _positions = new();

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

        private static DateTime _lastRateDate;
        private async Task StrategyAsync(CancellationToken cancellationToken)
        {
            var rates = (await GetRatesAsync(cancellationToken)).ToQuotes().ToArray();
            var tick = await GetTickAsync(cancellationToken);

            var isEndOfDay = _cycleProvider.Previous.TimeOfDay >= _end;

            var strategy = _operationSettings.Value.Strategy;
            var current = _positions.LastOrDefault(it => it.Volume() != 0);

            if (rates.Length < 3)
                return;

            var lastRate = rates[^1];
            if (_lastRateDate == lastRate.Date)
                return;
            _lastRateDate = lastRate.Date;

            var volume = rates[^2].Close > rates[^2].Open ? -strategy.Volume : strategy.Volume;

            if (current is not null)
            {
                var v = Math.Abs(current.Volume()) * strategy.IncrementVolume;

                v -= v % _operationSettings.Value.Symbol.StandardLot;

                if (volume > 0)
                    volume += v;
                else
                    volume -= v;
            }

            var beforeVolume = current is null ? 0 : current.Volume();
            var afterVolume = beforeVolume + volume;

            if (current is not null && afterVolume == 0)
                volume *= 2;

            if (current is not null && isEndOfDay)
                volume = current.Volume() * -1;

            if (volume == 0)
                return;

            var price = volume > 0 ? Convert.ToDecimal(tick.Trade.Ask) : Convert.ToDecimal(tick.Trade.Bid);
            var transaction = new Transaction(_cycleProvider.Previous, price, volume);

            if (current is null)
                _positions.Add(current = new Position());

            current.Add(transaction);

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
            if (_positions.Count == 0)
                return;

            var posVolume = 0M;
            var posProfit = 0M;
            var posPrice = 0M;

            foreach (var item in _positions)
            {
                var volume = item.Volume();
                var price = volume > 0 ? Convert.ToDecimal(tick.Trade.Ask) : Convert.ToDecimal(tick.Trade.Bid);

                posVolume += volume;
                posProfit += item.Profit(Convert.ToDecimal(price));
                posPrice = item.Price();
            }

            var symbol = _operationSettings.Value.Symbol;

            _logger.LogInformation("{@profit}", new
            {
                Time = transaction.Time.ToString("yyyy-MM-ddTHH:mm:ss.fff"),
                transaction.Price,
                Volume = Math.Round(transaction.Volume, symbol.VolumeDecimals),
                PosVolume = Math.Round(posVolume, symbol.VolumeDecimals),
                PosPrice = Math.Round(posPrice, symbol.PriceDecimals),
                PosProfit = Math.Round(posProfit, symbol.PriceDecimals),
            });
        }
    }
}
