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

        private readonly Backtest _backtest = new();

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

            var volume = 0d;

            if (volume > 0)
            {
                await BuyAsync(
                    volume,
                    cancellationToken);
            }

            if (volume < 0)
            {
                await SellAsync(
                     volume * -1,
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
                position.Volume!.Value :
                position.Volume!.Value * -1;

            return new Position(volume, position.Profit!.Value);
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
                //OpenPrice = Math.Round(result.OpenPrice, symbol.PriceDecimals),
                Profit = Math.Round(result.Profit, symbol.PriceDecimals),
            });
        }

        private record class Position(double Volume, double Profit);
    }
}
