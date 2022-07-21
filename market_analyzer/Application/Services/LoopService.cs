using Application.Helpers;
using Application.Options;
using Application.Services.Providers.Rates;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services
{
    public class LoopService : ILoopService
    {
        private readonly IRatesProvider _ratesProvider;
        private readonly IOrderManagementWrapper _orderManagementWrapper;
        private readonly IOrderCreator _orderCreator;
        private readonly IOptionsSnapshot<OperationSettings> _operationSettings;
        private readonly ILogger<ILoopService> _logger;
        private readonly TimeSpan _end;

        private readonly List<Range> _ranges = new();
        private int _rangesLastCount = 0;

        public LoopService(
            IRatesProvider ratesProvider,
            IOrderManagementWrapper orderManagementWrapper,
            IOrderCreator orderCreator,
            IOptionsSnapshot<OperationSettings> operationSettings,
            ILogger<ILoopService> logger)
        {
            _ratesProvider = ratesProvider;
            _orderManagementWrapper = orderManagementWrapper;
            _orderCreator = orderCreator;
            _operationSettings = operationSettings;
            _logger = logger;
            _end = _operationSettings.Value.End.ToTimeSpan().Subtract(TimeSpan.FromMinutes(1));
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var symbolData = _operationSettings.Value.SymbolData;

            await _ratesProvider.CheckNewRatesAsync(
                symbolData.Name!,
                symbolData.Date,
                symbolData.Timeframe,
                symbolData.ChunkSize,
                cancellationToken);

            var quotes = (await GetRatesAsync(cancellationToken)).ToQuotes().ToArray();

            if (!quotes.Any())
                return;

            UpdateRange(quotes);

            if (!await CanProceedAsync(cancellationToken))
            {
                _logger.LogInformation("Can't proceed!");

                return;
            }

            await CheckAsync(quotes, cancellationToken);
        }

        private async Task CheckAsync(CustomQuote[] quotes, CancellationToken cancellationToken)
        {
            var endOfDay = quotes.Last().Date.TimeOfDay >= _end;

            if (_ranges.Count == _rangesLastCount && !endOfDay)
                return;

            var current = await GetPositionAsync(cancellationToken);
            if (endOfDay && current is null)
                return;

            _rangesLastCount = _ranges.Count;

            if (_ranges.Count == 1)
                return;

            var last = _ranges[^1];
            var previous = _ranges[^2];

            var strategy = _operationSettings.Value.StrategyData;
            var isUp = (last.Value > previous.Value);
            var volume = isUp ? -strategy.Volume : strategy.Volume;

            if (current is not null)
            {
                var symbol = _operationSettings.Value.SymbolData;

                var moreVolume = Convert.ToDecimal(current.Volume) * strategy.MoreVolumeFactor;
                var mod = moreVolume % symbol.StandardLot;

                if (volume > 0)
                    volume += moreVolume - mod;
                else
                    volume -= moreVolume - mod;
            }

            if (current is not null)
            {
                var currentVolume = Convert.ToDecimal(current.Volume);

                if (current.Type == PositionType.Sell)
                    currentVolume *= -1;

                if (currentVolume + volume == 0)
                    volume *= 2;
            }

            if (current is not null && endOfDay)
                volume = Convert.ToDecimal(current.Volume) * -1;

            if (!_operationSettings.Value.ProductionMode)
            {
                _logger.LogInformation("{@data}", new { volume });

                return;
            }

            if (volume > 0)
            {
                await BuyAsync(
                    Convert.ToDouble(volume),
                    cancellationToken);
            }

            if (volume < 0)
            {
                await SellAsync(
                     Convert.ToDouble(volume * -1),
                     cancellationToken);
            }
        }

        private async Task<bool> CanProceedAsync(CancellationToken cancellationToken)
        {
            if (!_operationSettings.Value.ProductionMode)
                return true;

            var pendingOrders = await _orderManagementWrapper.GetOrdersAsync(
                group: _operationSettings.Value.SymbolData.Name,
                cancellationToken: cancellationToken);

            if (pendingOrders.ResponseStatus.ResponseCode != Res.SOk)
            {
                _logger.LogError("Grpc server error {@data}", new
                {
                    pendingOrders.ResponseStatus.ResponseCode,
                    pendingOrders.ResponseStatus.ResponseMessage
                });

                return false;
            }

            return !pendingOrders.Orders.Any();
        }

        private async Task BuyAsync(double volume, CancellationToken cancellationToken)
        {
            var tick = await _ratesProvider.GetSymbolTickAsync(_operationSettings.Value.SymbolData.Name!, cancellationToken);

            var request = _orderCreator.BuyAtMarket(
                symbol: _operationSettings.Value.SymbolData.Name!,
                price: tick.Trade.Bid!.Value,
                volume: volume,
                deviation: _operationSettings.Value.Order.Deviation,
                magic: _operationSettings.Value.Order.Magic);

            _logger.LogInformation("Buy Request {@request}", request);
            var response = await _orderManagementWrapper.SendOrderAsync(request, cancellationToken);
            _logger.LogInformation("Buy Reply {@response}", response);
        }

        private async Task SellAsync(double volume, CancellationToken cancellationToken)
        {
            var tick = await _ratesProvider.GetSymbolTickAsync(_operationSettings.Value.SymbolData.Name!, cancellationToken);

            var request = _orderCreator.SellAtMarket(
                symbol: _operationSettings.Value.SymbolData.Name!,
                price: tick.Trade.Ask!.Value,
                volume: volume,
                deviation: _operationSettings.Value.Order.Deviation,
                magic: _operationSettings.Value.Order.Magic);

            _logger.LogInformation("Sell Request {@request}", request);
            var response = await _orderManagementWrapper.SendOrderAsync(request, cancellationToken);
            _logger.LogInformation("Sell Reply {@response}", response);
        }

        private async Task<Grpc.Terminal.Position?> GetPositionAsync(CancellationToken cancellationToken)
        {
            var positions = await _orderManagementWrapper.GetPositionsAsync(
                group: _operationSettings.Value.SymbolData.Name!,
                cancellationToken: cancellationToken);

            return positions.Positions.FirstOrDefault();
        }

        private void UpdateRange(CustomQuote[] quotes)
        {
            if (!quotes.Any())
                return;

            if (!_ranges.Any())
                _ranges.Add(new(quotes.First().Open, quotes.First() with { }));

            var lastQuote = quotes.Last();
            var strategy = _operationSettings.Value.StrategyData;

            while (lastQuote.Close >= _ranges.Last().Value + strategy.RangePoints)
                _ranges.Add(new(_ranges.Last().Value + strategy.RangePoints, lastQuote with { }));

            while (lastQuote.Close <= _ranges.Last().Value - strategy.RangePoints)
                _ranges.Add(new(_ranges.Last().Value - strategy.RangePoints, lastQuote with { }));
        }

        private async Task<IEnumerable<Rate>> GetRatesAsync(CancellationToken cancellationToken)
        {
            var result = await _ratesProvider.GetRatesAsync(
                _operationSettings.Value.SymbolData.Name!,
                _operationSettings.Value.SymbolData.Date,
                _operationSettings.Value.SymbolData.Timeframe,
                _operationSettings.Value.SymbolData.Window,
                cancellationToken);

            return result.OrderBy(it => it.Time).ToArray();
        }
    }
}
