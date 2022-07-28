using Application.Helpers;
using Application.Options;
using Application.Services.Providers.Cycle;
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
        private readonly ICycleProvider _cycleProvider;
        private readonly IRatesProvider _ratesProvider;
        private readonly IOrderManagementWrapper _orderManagementWrapper;
        private readonly IOrderCreator _orderCreator;
        private readonly IOptions<OperationSettings> _operationSettings;
        private readonly ILogger<ILoopService> _logger;

        private readonly TimeSpan _end;
        private readonly Backtest _backtest = new();

        private bool _summaryPrinted = false;
        private DateTime _lastRateDate;

        public LoopService(
            ICycleProvider cycleProvider,
            IRatesProvider ratesProvider,
            IOrderManagementWrapper orderManagementWrapper,
            IOrderCreator orderCreator,
            IOptions<OperationSettings> operationSettings,
            ILogger<ILoopService> logger)
        {
            _cycleProvider = cycleProvider;
            _ratesProvider = ratesProvider;
            _orderManagementWrapper = orderManagementWrapper;
            _orderCreator = orderCreator;
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

            if (!await CanProceedAsync(cancellationToken))
            {
                _logger.LogInformation("Can't proceed!");

                return;
            }

            var rates = (await GetRatesAsync(cancellationToken)).ToQuotes().ToArray();

            var isEndOfDay = _cycleProvider.Now().TimeOfDay >= _end;
            var strategy = _operationSettings.Value.Strategy;
            var current = await GetPositionAsync(cancellationToken);
            var balance = current is not null ? Convert.ToDecimal(current.Profit) : 0;

            if (strategy.Profit is not null && balance >= strategy.Profit)
                isEndOfDay = true;

            if (current is null && isEndOfDay)
            {
                if (!_operationSettings.Value.ProductionMode && !_summaryPrinted)
                {
                    _summaryPrinted = true;
                    _logger.LogInformation("{@summary}", _backtest.Summary);
                }

                return;
            }

            //if (rates.Length < strategy.AtrLookbackPeriods + 1)
            //    return;

            //var lastRate = rates[^1];
            //if (_lastRateDate == lastRate.Date)
            //    return;
            //_lastRateDate = lastRate.Date;

            //var stopAtr = rates.GetVolatilityStop(strategy.AtrLookbackPeriods, strategy.AtrMultiplier);
            //var lastStopAtr = stopAtr.Last();

            var volume = 0m; //  lastStopAtr.LowerBand is not null ? -strategy.Volume : strategy.Volume;
            var beforeVolume = current is null ? 0 : current.Volume;
            var afterVolume = beforeVolume + volume;

            if (current is not null && afterVolume == 0)
                volume *= 2;

            if (current is not null && isEndOfDay)
                volume = beforeVolume * -1;

            if (volume == 0)
                return;

            if (!_operationSettings.Value.ProductionMode)
            {
                var bookPrice = await GetBookPriceAsync(cancellationToken);
                var transaction = _backtest.Execute(bookPrice, volume);

                Print(bookPrice, transaction);

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
                group: _operationSettings.Value.Symbol.Name,
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
            var tick = await _ratesProvider.GetSymbolTickAsync(_operationSettings.Value.Symbol.Name!, cancellationToken);

            var request = _orderCreator.BuyAtMarket(
                symbol: _operationSettings.Value.Symbol.Name!,
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
            var tick = await _ratesProvider.GetSymbolTickAsync(_operationSettings.Value.Symbol.Name!, cancellationToken);

            var request = _orderCreator.SellAtMarket(
                symbol: _operationSettings.Value.Symbol.Name!,
                price: tick.Trade.Ask!.Value,
                volume: volume,
                deviation: _operationSettings.Value.Order.Deviation,
                magic: _operationSettings.Value.Order.Magic);

            _logger.LogInformation("Sell Request {@request}", request);
            var response = await _orderManagementWrapper.SendOrderAsync(request, cancellationToken);
            _logger.LogInformation("Sell Reply {@response}", response);
        }

        private async Task<Position?> GetPositionAsync(CancellationToken cancellationToken)
        {
            if (!_operationSettings.Value.ProductionMode)
            {
                var simPosition = _backtest.OpenPosition();

                if (simPosition is null)
                    return null;

                var bookPrice = await GetBookPriceAsync(cancellationToken);
                var balance = _backtest.Balance(bookPrice);

                return new Position(balance.OpenVolume, balance.Profit);
            }

            var positions = await _orderManagementWrapper.GetPositionsAsync(
                group: _operationSettings.Value.Symbol.Name!,
                cancellationToken: cancellationToken);

            var position = positions.Positions.FirstOrDefault();

            if (position is null)
                return null;

            var volume = position.Type == PositionType.Buy ?
                Convert.ToDecimal(position.Volume) :
                Convert.ToDecimal(position.Volume) * -1;

            return new Position(volume, Convert.ToDecimal(position.Profit));
        }

        private async Task<BookPrice> GetBookPriceAsync(CancellationToken cancellationToken)
        {
            var tick = await _ratesProvider.GetSymbolTickAsync(_operationSettings.Value.Symbol.Name!, cancellationToken);
            return new BookPrice(tick.Trade.Time.ToDateTime(), Convert.ToDecimal(tick.Trade.Bid), Convert.ToDecimal(tick.Trade.Ask));
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

        private record class Position(decimal Volume, decimal Profit);
    }
}
