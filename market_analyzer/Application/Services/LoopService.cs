using Application.Helpers;
using Application.Options;
using Application.Services.Providers.Date;
using Application.Services.Providers.Rates;
using Grpc.Terminal.Enums;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OoplesFinance.StockIndicators.Models;

namespace Application.Services;

public class LoopService : ILoopService
{
    private readonly IDateProvider _dateProvider;
    private readonly IRatesProvider _ratesProvider;
    private readonly IOrderManagementSystemWrapper _orderManagementSystemWrapper;
    private readonly IOrderCreator _orderCreator;
    private readonly IOptions<OperationSettings> _operationSettings;
    private readonly ILogger<ILoopService> _logger;

    public LoopService(
        IDateProvider dateProvider,
        IRatesProvider ratesProvider,
        IOrderManagementSystemWrapper orderManagementSystemWrapper,
        IOrderCreator orderCreator,
        IOptions<OperationSettings> operationSettings,
        ILogger<ILoopService> logger)
    {
        _dateProvider = dateProvider;
        _ratesProvider = ratesProvider;
        _orderManagementSystemWrapper = orderManagementSystemWrapper;
        _orderCreator = orderCreator;
        _operationSettings = operationSettings;
        _logger = logger;

        _ratesProvider.Initialize(
            _operationSettings.Value.Symbol.Name!,
            _operationSettings.Value.Date,
            _operationSettings.Value.Timeframe,
            _operationSettings.Value.StreamingData.ChunkSize);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await _ratesProvider.UpdateRatesAsync(cancellationToken);

        if (!await CanProceedAsync(cancellationToken))
        {
            _logger.LogInformation("Can't proceed!");

            return;
        }

        var quotes = await GetQuotes(cancellationToken);
        var tickerDatas = await GetTickerDatas(cancellationToken);

        var isEndOfDay = IsEndOfDay();

        var current = await GetPositionAsync(cancellationToken);

        if (current is null && isEndOfDay)
        {
            return;
        }

        var volume = 0m;
        var beforeVolume = current is null ? 0 : current.Volume;
        var afterVolume = beforeVolume + volume;

        if (current is not null && afterVolume == 0)
            volume *= 2;

        if (current is not null && isEndOfDay)
            volume = beforeVolume * -1;

        if (volume == 0)
            return;

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

    private bool IsEndOfDay()
    {
        return _dateProvider.LocalDateSpecifiedUtcKind().TimeOfDay >= _operationSettings.Value.End.ToTimeSpan();
    }

    private async Task<bool> CanProceedAsync(CancellationToken cancellationToken)
    {
        if (!_operationSettings.Value.ProductionMode)
        {
            return true;
        }

        var pendingOrders = await _orderManagementSystemWrapper.GetOrdersAsync(
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
        var tick = await _ratesProvider.GetSymbolTickAsync(cancellationToken);

        var request = _orderCreator.BuyAtMarket(
            symbol: _operationSettings.Value.Symbol.Name!,
            price: tick.Trade.Bid!.Value,
            volume: volume,
            deviation: _operationSettings.Value.Order.Deviation,
            magic: _operationSettings.Value.Order.Magic);

        _logger.LogInformation("Buy Request {@request}", request);

        var response = await _orderManagementSystemWrapper.SendOrderAsync(request, cancellationToken);

        _logger.LogInformation("Buy Reply {@response}", response);
    }

    private async Task SellAsync(double volume, CancellationToken cancellationToken)
    {
        var tick = await _ratesProvider.GetSymbolTickAsync(cancellationToken);

        var request = _orderCreator.SellAtMarket(
            symbol: _operationSettings.Value.Symbol.Name!,
            price: tick.Trade.Ask!.Value,
            volume: volume,
            deviation: _operationSettings.Value.Order.Deviation,
            magic: _operationSettings.Value.Order.Magic);

        _logger.LogInformation("Sell Request {@request}", request);

        var response = await _orderManagementSystemWrapper.SendOrderAsync(request, cancellationToken);

        _logger.LogInformation("Sell Reply {@response}", response);
    }

    private async Task<Position?> GetPositionAsync(CancellationToken cancellationToken)
    {
        var positions = await _orderManagementSystemWrapper.GetPositionsAsync(
            group: _operationSettings.Value.Symbol.Name!,
            cancellationToken: cancellationToken);

        var position = positions.Positions.FirstOrDefault();

        if (position is null)
        {
            return null;
        }

        return new Position(position);
    }

    private async Task<IEnumerable<CustomQuote>> GetQuotes(CancellationToken cancellationToken)
    {
        var rates = await _ratesProvider.GetRatesAsync(cancellationToken);
        return rates.ToQuotes().ToArray();
    }

    private async Task<IEnumerable<TickerData>> GetTickerDatas(CancellationToken cancellationToken)
    {
        var rates = await _ratesProvider.GetRatesAsync(cancellationToken);
        return rates.ToTickerData().ToArray();
    }

    private class Position(Grpc.Terminal.Position position)
    {
        public decimal Volume =>
            position.Type == PositionType.Buy ?
                Convert.ToDecimal(position.Volume) :
                Convert.ToDecimal(position.Volume) * -1;

        public decimal Profit => Convert.ToDecimal(position.Profit);
    }
}
