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

        private readonly decimal _points = 200;
        private readonly Dictionary<decimal, decimal> _incrementVolume = new()
        {
            { 1M, 1M },
        };

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
            var endOfDay = quotes.Last().Date.TimeOfDay >=
                _operationSettings.Value.End.ToTimeSpan().Subtract(TimeSpan.FromMinutes(1));

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

            var value = _incrementVolume.First();
            var volume = last.Value > previous.Value ? -value.Value : value.Value; // up: sell, down: buy

            if (current is not null)
            {
                var currentVolume = Convert.ToDecimal(current.Type == PositionType.Buy ? current.Volume : current.Volume * -1);

                if (endOfDay)
                    volume = currentVolume * -1; // close
                else if (currentVolume < 0 && volume > 0 || currentVolume > 0 && volume < 0)
                    volume += currentVolume * -1; // invert
                else
                {
                    // increment (avg price)
                    value = _incrementVolume.Last(it => it.Key <= Math.Abs(currentVolume));
                    volume = last.Value > previous.Value ? -value.Value : value.Value;
                }
            }

            if (_operationSettings.Value.ProductionMode)
            {
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
        }

        private async Task<bool> CanProceedAsync(CancellationToken cancellationToken)
        {
            if (!_operationSettings.Value.ProductionMode)
                return true;

            var pendingOrders = await _orderManagementWrapper.GetOrdersAsync(
                group: _operationSettings.Value.MarketData.Symbol,
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
            var tick = await _ratesProvider.GetSymbolTickAsync(_operationSettings.Value.MarketData.Symbol!, cancellationToken);

            var request = _orderCreator.BuyAtMarket(
                symbol: _operationSettings.Value.MarketData.Symbol!,
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
            var tick = await _ratesProvider.GetSymbolTickAsync(_operationSettings.Value.MarketData.Symbol!, cancellationToken);

            var request = _orderCreator.SellAtMarket(
                symbol: _operationSettings.Value.MarketData.Symbol!,
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
                group: _operationSettings.Value.MarketData.Symbol!,
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

            while (lastQuote.Close >= _ranges.Last().Value + _points)
                _ranges.Add(new(_ranges.Last().Value + _points, lastQuote with { }));

            while (lastQuote.Close <= _ranges.Last().Value - _points)
                _ranges.Add(new(_ranges.Last().Value - _points, lastQuote with { }));
        }

        private async Task<IEnumerable<Rate>> GetRatesAsync(CancellationToken cancellationToken)
        {
            var result = await _ratesProvider.GetRatesAsync(
                _operationSettings.Value.MarketData.Symbol!,
                _operationSettings.Value.MarketData.Date,
                _operationSettings.Value.MarketData.Timeframe,
                _operationSettings.Value.MarketData.Window,
                cancellationToken);

            return result.OrderBy(it => it.Time).ToArray();
        }
    }
}
