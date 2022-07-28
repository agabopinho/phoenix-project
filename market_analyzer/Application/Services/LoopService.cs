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
        private readonly IOptions<OperationSettings> _operationSettings;
        private readonly ILogger<ILoopService> _logger;
        private readonly TimeSpan _end;

        private readonly List<Range> _ranges = new();
        private int _rangesLastCount = 0;

        public LoopService(
            IRatesProvider ratesProvider,
            IOrderManagementWrapper orderManagementWrapper,
            IOrderCreator orderCreator,
            IOptions<OperationSettings> operationSettings,
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
            var symbolData = _operationSettings.Value.Symbol;

            await _ratesProvider.CheckNewRatesAsync(
                symbolData.Name!,
                _operationSettings.Value.Date,
                _operationSettings.Value.Timeframe,
                _operationSettings.Value.StreamingData.ChunkSize,
                cancellationToken);

            var quotes = (await GetRatesAsync(cancellationToken)).ToQuotes().ToArray();

            if (!quotes.Any())
                return;

            if (!await CanProceedAsync(cancellationToken))
            {
                _logger.LogInformation("Can't proceed!");

                return;
            }

            await CheckAsync(quotes, cancellationToken);
        }

        private async Task CheckAsync(CustomQuote[] quotes, CancellationToken cancellationToken)
        {
            var volume = 0M;

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

        private async Task<Grpc.Terminal.Position?> GetPositionAsync(CancellationToken cancellationToken)
        {
            var positions = await _orderManagementWrapper.GetPositionsAsync(
                group: _operationSettings.Value.Symbol.Name!,
                cancellationToken: cancellationToken);

            return positions.Positions.FirstOrDefault();
        }

        private async Task<IEnumerable<Rate>> GetRatesAsync(CancellationToken cancellationToken)
        {
            var result = await _ratesProvider.GetRatesAsync(
                _operationSettings.Value.Symbol.Name!,
                _operationSettings.Value.Date,
                _operationSettings.Value.Timeframe,
                _operationSettings.Value.Window,
                cancellationToken);

            return result.OrderBy(it => it.Time).ToArray();
        }
    }
}
