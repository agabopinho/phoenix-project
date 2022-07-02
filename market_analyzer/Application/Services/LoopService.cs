﻿using Application.Helpers;
using Application.Options;
using Application.Services.Providers.Rates;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OoplesFinance.StockIndicators;
using OoplesFinance.StockIndicators.Enums;
using OoplesFinance.StockIndicators.Models;

namespace Application.Services
{
    public class LoopService : ILoopService
    {
        private readonly IRatesProvider _ratesProvider;
        private readonly IOrderManagementWrapper _orderManagementWrapper;
        private readonly IOrderCreator _orderCreator;
        private readonly IOptionsSnapshot<OperationSettings> _operationSettings;
        private readonly ILogger<ILoopService> _logger;

        private Signal _lastSignal = Signal.None;
        private DateTime _lastSinalDate = DateTime.MinValue;

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

            if (!await CanProceedAsync(cancellationToken))
            {
                _logger.LogInformation("Can't proceed!");

                return;
            }

            var rates = await GetRatesAsync(cancellationToken);
            var tickerData = rates.ToTickerData();
            var stockData = new StockData(tickerData)
               .CalculateGannHiLoActivator(length: _operationSettings.Value.Indicator.Length);

            if (stockData.Count == 0)
            {
                _logger.LogWarning("No market data!");

                return;
            }

            await CheckSignalAsync(stockData, cancellationToken);
        }

        private async Task<bool> CanProceedAsync(CancellationToken cancellationToken)
        {
            if (!_operationSettings.Value.ProductionMode)
                return true;

            var pendingOrders = await _orderManagementWrapper.GetOrdersAsync(
                group: _operationSettings.Value.MarketData.Symbol, cancellationToken: cancellationToken);

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

        private async Task CheckSignalAsync(StockData stockData, CancellationToken cancellationToken)
        {
            var current = stockData.SignalsList[^_operationSettings.Value.Indicator.SignalShift];
            var date = stockData.Dates.Last();

            if (!HasChanged(date, current))
                return;

            _lastSignal = current;
            _lastSinalDate = date;

            var price = stockData.ClosePrices.Last();

            if (!_operationSettings.Value.ProductionMode)
            {
                var tick = await _ratesProvider.GetSymbolTickAsync(_operationSettings.Value.MarketData.Symbol!, cancellationToken);

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
                Signal = current
            });

            if (_operationSettings.Value.ProductionMode)
                await CheckPositionAsync(cancellationToken);
        }

        private bool HasChanged(DateTime date, Signal current)
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
            var positions = await GetPositionsAsync(_operationSettings.Value.MarketData.Symbol!, cancellationToken);

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
                    _operationSettings.Value.MarketData.Symbol!,
                    volume,
                    _operationSettings.Value.Order.Deviation,
                    _operationSettings.Value.Order.Magic,
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
                    _operationSettings.Value.MarketData.Symbol!,
                    volume,
                    _operationSettings.Value.Order.Deviation,
                    _operationSettings.Value.Order.Magic,
                    cancellationToken);

                return;
            }
        }

        private async Task BuyAsync(string symbol, double volume, int deviation, long magic, CancellationToken cancellationToken)
        {
            var tick = await _ratesProvider.GetSymbolTickAsync(symbol, cancellationToken);

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
            var tick = await _ratesProvider.GetSymbolTickAsync(symbol, cancellationToken);

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

        private async Task<IEnumerable<Rate>> GetRatesAsync(CancellationToken cancellationToken)
        {
            var result = await _ratesProvider.GetRatesAsync(
                _operationSettings.Value.MarketData.Symbol!,
                _operationSettings.Value.MarketData.Date,
                _operationSettings.Value.MarketData.Timeframe,
                _operationSettings.Value.Indicator.Window,
                cancellationToken);

            return result.OrderBy(it => it.Time);
        }
    }
}
