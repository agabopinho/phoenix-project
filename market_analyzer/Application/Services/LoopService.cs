using Application.Helpers;
using Grpc.Core;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Logging;
using OoplesFinance.StockIndicators;
using OoplesFinance.StockIndicators.Enums;
using OoplesFinance.StockIndicators.Models;
using System.Diagnostics;

namespace Application.Services
{
    public static class Operation
    {
        public static readonly string Symbol = "WINQ22";
        public static readonly DateOnly Date = new(2022, 6, 30);
        public static readonly int ChunkSize = 5000;
        public static readonly TimeSpan Timeframe = TimeSpan.FromSeconds(10);
        public static readonly int Deviation = 10;
        public static readonly long Magic = 467276;
        public static readonly bool ExecOrder = false;
    }

    public interface ILoopService
    {
        Task RunAsync(CancellationToken cancellationToken);
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

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            await _ratesStateService.CheckNewRatesAsync(
                Operation.Symbol, Operation.Date, Operation.Timeframe,
                Operation.ChunkSize, cancellationToken);

            if (!await CanProceedAsync(cancellationToken))
            {
                _logger.LogInformation("Can't proceed!");

                return;
            }

            var rates = await GetRatesAsync(cancellationToken);
            var tickerData = rates.ToTickerData();

            var stockData = new StockData(tickerData)
                .CalculateGannHiLoActivator();

            await CheckSignalAsync(stockData, cancellationToken);
        }

        private async Task<bool> CanProceedAsync(CancellationToken cancellationToken)
        {
            if (!Operation.ExecOrder)
                return true;

            var pendingOrders = await _orderManagementWrapper.GetOrdersAsync(
                group: Operation.Symbol, cancellationToken: cancellationToken);

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

        private async Task CheckSignalAsync(StockData stockData, CancellationToken cancellationToken)
        {
            var current = stockData.SignalsList[^2];

            if (!HasChanged(current))
                return;

            _lastSignal = current;

            var price = stockData.ClosePrices.Last();

            if (!Operation.ExecOrder)
            {
                var tick = await _marketDataWrapper.GetSymbolTickAsync(Operation.Symbol, cancellationToken);

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

            if (Operation.ExecOrder)
                await CheckPositionAsync(cancellationToken);
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

        private async Task CheckPositionAsync(CancellationToken cancellationToken)
        {
            var positions = await GetPositionsAsync(cancellationToken);

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

                await BuyAsync(volume, cancellationToken);

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

                await SellAsync(volume, cancellationToken);

                return;
            }
        }

        private async Task BuyAsync(double volume, CancellationToken cancellationToken)
        {
            var tick = await _marketDataWrapper.GetSymbolTickAsync(Operation.Symbol, cancellationToken);

            var request = _orderCreator.BuyAtMarket(
                symbol: Operation.Symbol,
                price: tick.Trade.Bid!.Value,
                volume: volume,
                deviation: Operation.Deviation,
                magic: Operation.Magic);

            _logger.LogInformation("Buy Request {@request}", request);
            var response = await _orderManagementWrapper.SendOrderAsync(request, cancellationToken);
            _logger.LogInformation("Buy Reply {@response}", response);
        }

        private async Task SellAsync(double volume, CancellationToken cancellationToken)
        {
            var tick = await _marketDataWrapper.GetSymbolTickAsync(Operation.Symbol, cancellationToken);

            var request = _orderCreator.SellAtMarket(
                symbol: Operation.Symbol,
                price: tick.Trade.Ask!.Value,
                volume: volume,
                deviation: 10,
                magic: Operation.Magic);

            _logger.LogInformation("Sell Request {@request}", request);
            var response = await _orderManagementWrapper.SendOrderAsync(request, cancellationToken);
            _logger.LogInformation("Sell Reply {@response}", response);
        }

        private async Task<GetPositionsReply> GetPositionsAsync(CancellationToken cancellationToken)
            => await _orderManagementWrapper.GetPositionsAsync(
                group: Operation.Symbol,
                cancellationToken: cancellationToken);

        private async Task<IEnumerable<Rate>> GetRatesAsync(CancellationToken cancellationToken)
        {
            var result = await _ratesStateService.GetRatesAsync(
                Operation.Symbol,
                Operation.Date,
                Operation.Timeframe,
                TimeSpan.FromMinutes(5),
                cancellationToken);

            return result.OrderBy(it => it.Time);
        }
    }
}
