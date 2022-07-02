using Application.Helpers;
using Application.Options;
using Application.Services.Providers.Rates;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Logging;
using OoplesFinance.StockIndicators;
using OoplesFinance.StockIndicators.Enums;
using OoplesFinance.StockIndicators.Models;

namespace Application.Services
{
    public class LoopService : ILoopService
    {
        private readonly IRatesProvider _ratesStateService;
        private readonly IOrderManagementWrapper _orderManagementWrapper;
        private readonly IOrderCreator _orderCreator;
        private readonly ILogger<ILoopService> _logger;

        private Signal _lastSignal = Signal.None;
        private DateTime _lastSinalDate = DateTime.MinValue;

        public LoopService(
            IRatesProvider ratesStateService,
            IOrderManagementWrapper orderManagementWrapper,
            IOrderCreator orderCreator,
            ILogger<ILoopService> logger)
        {
            _ratesStateService = ratesStateService;
            _orderManagementWrapper = orderManagementWrapper;
            _orderCreator = orderCreator;
            _logger = logger;
        }

        public async Task RunAsync(OperationSettings operationSettings, CancellationToken cancellationToken)
        {
            await _ratesStateService.CheckNewRatesAsync(
                operationSettings.MarketData.Symbol!,
                operationSettings.MarketData.Date,
                operationSettings.MarketData.Timeframe,
                operationSettings.Infra.ChunkSize,
                cancellationToken);

            if (!await CanProceedAsync(operationSettings, cancellationToken))
            {
                _logger.LogInformation("Can't proceed!");

                return;
            }

            var rates = await GetRatesAsync(operationSettings, cancellationToken);
            var tickerData = rates.ToTickerData();
            var stockData = new StockData(tickerData)
                .CalculateGannHiLoActivator(length: operationSettings.Indicator.Length);

            if (stockData.Count == 0)
            {
                _logger.LogWarning("No market data!");

                return;
            }

            await CheckSignalAsync(operationSettings, stockData, cancellationToken);
        }

        private async Task<bool> CanProceedAsync(OperationSettings operationSettings, CancellationToken cancellationToken)
        {
            if (!operationSettings.ProductionMode)
                return true;

            var pendingOrders = await _orderManagementWrapper.GetOrdersAsync(
                group: operationSettings.MarketData.Symbol, cancellationToken: cancellationToken);

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

        private async Task CheckSignalAsync(OperationSettings operationSettings, StockData stockData, CancellationToken cancellationToken)
        {
            var current = stockData.SignalsList[^operationSettings.Indicator.SignalShift];
            var date = stockData.Dates.Last();

            if (!HasChanged(date, current))
                return;

            _lastSignal = current;
            _lastSinalDate = date;

            var price = stockData.ClosePrices.Last();

            if (!operationSettings.ProductionMode)
            {
                var tick = await _ratesStateService.GetSymbolTickAsync(operationSettings.MarketData.Symbol!, cancellationToken);

                date = tick.Trade.Time.ToDateTime();

                if (_lastSignal.IsSignalBuy())
                    price = Convert.ToDecimal(tick.Trade.Ask!.Value);

                if (_lastSignal.IsSignalSell())
                    price = Convert.ToDecimal(tick.Trade.Bid!.Value);
            }

            _logger.LogInformation("{@data}", new
            {
                Date = date,
                Price = price,
                Ghla = stockData.OutputValues["Ghla"].Last(),
                Signal = current
            });

            if (operationSettings.ProductionMode)
                await CheckPositionAsync(operationSettings, cancellationToken);
        }

        private bool HasChanged(DateTime date, Signal current)
        {
            if (_lastSinalDate == date)
                return false;

            if (current.IsNone())
                return false;

            if (_lastSignal.IsNone())
                return true;

            return _lastSignal.IsSignalBuy() && !current.IsSignalBuy() ||
                   _lastSignal.IsSignalSell() && !current.IsSignalSell();
        }

        private async Task CheckPositionAsync(OperationSettings operationSettings, CancellationToken cancellationToken)
        {
            var positions = await GetPositionsAsync(operationSettings.MarketData.Symbol!, cancellationToken);

            if (_lastSignal.IsSignalBuy())
            {
                if (positions.Positions.Any(it => it.Type == PositionType.Buy))
                    return;

                var volume = 1d;

                if (positions.Positions.Any(it => it.Type == PositionType.Sell))
                {
                    var sellPosition = positions.Positions.First(it => it.Type == PositionType.Sell);
                    volume = sellPosition.Volume!.Value * 2;
                }

                await BuyAsync(
                    operationSettings.MarketData.Symbol!,
                    volume,
                    operationSettings.Order.Deviation,
                    operationSettings.Order.Magic,
                    cancellationToken);

                return;
            }

            if (_lastSignal.IsSignalSell())
            {
                if (positions.Positions.Any(it => it.Type == PositionType.Sell))
                    return;

                var volume = 1d;

                if (positions.Positions.Any(it => it.Type == PositionType.Buy))
                {
                    var buyPosition = positions.Positions.First(it => it.Type == PositionType.Buy);
                    volume = buyPosition.Volume!.Value * 2;
                }

                await SellAsync(
                    operationSettings.MarketData.Symbol!,
                    volume,
                    operationSettings.Order.Deviation,
                    operationSettings.Order.Magic,
                    cancellationToken);

                return;
            }
        }

        private async Task BuyAsync(string symbol, double volume, int deviation, long magic, CancellationToken cancellationToken)
        {
            var tick = await _ratesStateService.GetSymbolTickAsync(symbol, cancellationToken);

            var request = _orderCreator.BuyAtMarket(
                symbol: symbol,
                price: tick.Trade.Bid!.Value,
                volume: volume,
                deviation: deviation,
                magic: magic);

            _logger.LogInformation("Buy Request {@request}", request);
            var response = await _orderManagementWrapper.SendOrderAsync(request, cancellationToken);
            _logger.LogInformation("Buy Reply {@response}", response);
        }

        private async Task SellAsync(string symbol, double volume, int deviation, long magic, CancellationToken cancellationToken)
        {
            var tick = await _ratesStateService.GetSymbolTickAsync(symbol, cancellationToken);

            var request = _orderCreator.SellAtMarket(
                symbol: symbol,
                price: tick.Trade.Ask!.Value,
                volume: volume,
                deviation: deviation,
                magic: magic);

            _logger.LogInformation("Sell Request {@request}", request);
            var response = await _orderManagementWrapper.SendOrderAsync(request, cancellationToken);
            _logger.LogInformation("Sell Reply {@response}", response);
        }

        private async Task<GetPositionsReply> GetPositionsAsync(string symbol, CancellationToken cancellationToken)
            => await _orderManagementWrapper.GetPositionsAsync(
                group: symbol,
                cancellationToken: cancellationToken);

        private async Task<IEnumerable<Rate>> GetRatesAsync(OperationSettings operationSettings, CancellationToken cancellationToken)
        {
            var result = await _ratesStateService.GetRatesAsync(
                operationSettings.MarketData.Symbol!,
                operationSettings.MarketData.Date,
                operationSettings.MarketData.Timeframe,
                operationSettings.Indicator.Window,
                cancellationToken);

            return result.OrderBy(it => it.Time);
        }
    }
}
