using Application.Helpers;
using Application.Options;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Logging;
using OoplesFinance.StockIndicators;
using OoplesFinance.StockIndicators.Enums;
using OoplesFinance.StockIndicators.Models;

namespace Application.Services
{

    public interface ILoopService
    {
        Task RunAsync(OperationSettings operationSettings, CancellationToken cancellationToken);
    }

    public class LoopService : ILoopService
    {
        private Signal _lastSignal = Signal.None;

        private readonly IRatesStateService _ratesStateService;
        private readonly IMarketDataWrapper _marketDataWrapper;
        private readonly IOrderManagementWrapper _orderManagementWrapper;
        private readonly IOrderCreator _orderCreator;
        private readonly ILogger<ILoopService> _logger;

        public LoopService(
            IRatesStateService ratesStateService,
            IMarketDataWrapper marketDataWrapper,
            IOrderManagementWrapper orderManagementWrapper,
            IOrderCreator orderCreator,
            ILogger<ILoopService> logger)
        {
            _ratesStateService = ratesStateService;
            _marketDataWrapper = marketDataWrapper;
            _orderManagementWrapper = orderManagementWrapper;
            _orderCreator = orderCreator;
            _logger = logger;
        }

        public async Task RunAsync(OperationSettings operationSettings, CancellationToken cancellationToken)
        {
            await _ratesStateService.CheckNewRatesAsync(
                operationSettings.Symbol!,
                operationSettings.Date,
                operationSettings.Timeframe,
                operationSettings.ChunkSize,
                cancellationToken);

            if (!await CanProceedAsync(operationSettings, cancellationToken))
            {
                _logger.LogInformation("Can't proceed!");

                return;
            }

            var rates = await GetRatesAsync(operationSettings, cancellationToken);
            var tickerData = rates.ToTickerData();

            var stockData = new StockData(tickerData)
                .CalculateGannHiLoActivator();

            await CheckSignalAsync(operationSettings, stockData, cancellationToken);
        }

        private async Task<bool> CanProceedAsync(OperationSettings operationSettings, CancellationToken cancellationToken)
        {
            if (!operationSettings.ExecOrder)
                return true;

            var pendingOrders = await _orderManagementWrapper.GetOrdersAsync(
                group: operationSettings.Symbol, cancellationToken: cancellationToken);

            if (pendingOrders.ResponseCode != Res.SOk)
            {
                _logger.LogError("Grpc server error {@data}", new
                {
                    pendingOrders.ResponseCode,
                    pendingOrders.ResponseMessage
                });

                return false;
            }

            return !pendingOrders.Orders.Any();
        }

        private async Task CheckSignalAsync(OperationSettings operationSettings, StockData stockData, CancellationToken cancellationToken)
        {
            var current = stockData.SignalsList[^2];

            if (!HasChanged(current))
                return;

            _lastSignal = current;

            var price = stockData.ClosePrices.Last();

            if (!operationSettings.ExecOrder)
            {
                var tick = await _marketDataWrapper.GetSymbolTickAsync(operationSettings.Symbol!, cancellationToken);

                if (_lastSignal.IsSignalBuy())
                    price = Convert.ToDecimal(tick.Trade.Ask!.Value);

                if (_lastSignal.IsSignalSell())
                    price = Convert.ToDecimal(tick.Trade.Bid!.Value);
            }

            _logger.LogInformation("{@data}", new
            {
                Date = stockData.Dates.Last(),
                Price = price,
                Ghla = stockData.OutputValues["Ghla"].Last(),
                Signal = current
            });

            if (operationSettings.ExecOrder)
                await CheckPositionAsync(operationSettings, cancellationToken);
        }

        private bool HasChanged(Signal current)
        {
            if (current.IsNone())
                return false;

            if (_lastSignal.IsNone())
                return true;

            return _lastSignal.IsSignalBuy() && !current.IsSignalBuy() ||
                   _lastSignal.IsSignalSell() && !current.IsSignalSell();
        }

        private async Task CheckPositionAsync(OperationSettings operationSettings, CancellationToken cancellationToken)
        {
            var positions = await GetPositionsAsync(operationSettings.Symbol!, cancellationToken);

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
                    operationSettings.Symbol!,
                    volume, operationSettings.Deviation,
                    operationSettings.Magic,
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
                    operationSettings.Symbol!,
                    volume, operationSettings.Deviation,
                    operationSettings.Magic,
                    cancellationToken);

                return;
            }
        }

        private async Task BuyAsync(string symbol, double volume, int deviation, long magic, CancellationToken cancellationToken)
        {
            var tick = await _marketDataWrapper.GetSymbolTickAsync(symbol, cancellationToken);

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
            var tick = await _marketDataWrapper.GetSymbolTickAsync(symbol, cancellationToken);

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
                operationSettings.Symbol!,
                operationSettings.Date,
                operationSettings.Timeframe,
                operationSettings.Window,
                cancellationToken);

            return result.OrderBy(it => it.Time);
        }
    }
}
