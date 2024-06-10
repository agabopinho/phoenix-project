using Application.Options;
using Application.Range;
using Application.Services.Providers.Date;
using Grpc.Core;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace Application.Services;

public class LoopService(
    IMarketDataWrapper marketDataWrapper,
    IOrderManagementSystemWrapper orderManagementSystemWrapper,
    IOrderCreator orderCreator,
    IDateProvider dateProvider,
    IOptionsMonitor<OperationSettings> operationSettings,
    ILogger<ILoopService> logger) : ILoopService
{
    private const int AHEAD_SECONDS = 30;

    private readonly IMarketDataWrapper _marketDataWrapper = marketDataWrapper;
    private readonly IOrderManagementSystemWrapper _orderManagementSystemWrapper = orderManagementSystemWrapper;
    private readonly IOrderCreator _orderCreator = orderCreator;
    private readonly IDateProvider _dateProvider = dateProvider;
    private readonly IOptionsMonitor<OperationSettings> _operationSettings = operationSettings;
    private readonly ILogger<ILoopService> _logger = logger;
    private readonly RangeCalculation _rangeCalculation = new(operationSettings.CurrentValue.BrickSize!.Value);
    private readonly List<GrpcError> _responseStatus = [];

    private DateTime _time;
    private Trade? _lastTrade;
    private int _previousBricksCount;
    private int _newBricks;

    private void PreExecution()
    {
        _time = _dateProvider.LocalDateSpecifiedUtcKind();
        _responseStatus.Clear();
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        PreExecution();

        await CheckNewPrice(cancellationToken);

        if (_newBricks > 0)
        {
            _logger.LogInformation("NewBricks: {newBricks}", _newBricks);
        }

        var current = await GetPositionAsync(cancellationToken);

        if (current is null)
        {
            return;
        }
    }

    private async Task CheckNewPrice(CancellationToken cancellationToken)
    {
        _previousBricksCount = _rangeCalculation.Bricks.Count;

        var fromDate = _lastTrade?.Time.ToDateTime() ?? (_time - _time.TimeOfDay);
        var toDate = _time.AddSeconds(AHEAD_SECONDS);

        if (_previousBricksCount == 0)
        {
            _logger.LogInformation("Loading data from: {fromDate}", fromDate);
        }

        var ticksReply = _marketDataWrapper.StreamTicksRange(
            _operationSettings.CurrentValue.Symbol!,
            fromDate,
            toDate,
            CopyTicks.Trade,
            _operationSettings.CurrentValue.StreamingData.ChunkSize, cancellationToken);

        var lastTrade = default(Trade);

        await foreach (var ticksRangeReply in ticksReply.ResponseStream.ReadAllAsync(cancellationToken))
        {
            CheckResponseStatus(ResponseType.GetTicks, ticksRangeReply.ResponseStatus);

            if (ticksRangeReply.Trades is null)
            {
                continue;
            }

            foreach (var trade in ticksRangeReply.Trades)
            {
                if (!IsNewTrade(trade))
                {
                    continue;
                }

                lastTrade = trade;

                _rangeCalculation.CheckNewPrice(trade.Time.ToDateTime(), trade.Last!.Value, trade.Volume!.Value);
            }
        }

        if (lastTrade is not null)
        {
            _lastTrade = lastTrade;
        }

        _newBricks = _rangeCalculation.Bricks.Count - _previousBricksCount;
    }

    private bool IsNewTrade(Trade trade)
    {
        if (_lastTrade is null)
        {
            return true;
        }

        if (trade.Time.ToDateTime() < _lastTrade.Time.ToDateTime())
        {
            return false;
        }

        if (trade.Time.ToDateTime() == _lastTrade.Time.ToDateTime() &&
            trade.Last == _lastTrade.Last &&
            trade.Flags == _lastTrade.Flags &&
            trade.Volume == _lastTrade.Volume)
        {
            return false;
        }

        return true;
    }

    private async Task<IEnumerable<Order>> GetOrdersAsync(CancellationToken cancellationToken)
    {
        var orders = await _orderManagementSystemWrapper.GetOrdersAsync(
            group: _operationSettings.CurrentValue.Symbol,
            cancellationToken: cancellationToken);

        CheckResponseStatus(ResponseType.GetOrder, orders.ResponseStatus);

        return orders.Orders ?? [];
    }

    private async Task<Position?> GetPositionAsync(CancellationToken cancellationToken)
    {
        var positions = await _orderManagementSystemWrapper.GetPositionsAsync(
            group: _operationSettings.CurrentValue.Symbol!,
            cancellationToken: cancellationToken);

        CheckResponseStatus(ResponseType.GetPosition, positions.ResponseStatus);

        return positions.Positions?.FirstOrDefault();
    }

    private void CheckResponseStatus(ResponseType type, ResponseStatus responseStatus)
    {
        if (responseStatus.ResponseCode == Res.SOk)
        {
            return;
        }

        _responseStatus.Add(new(_dateProvider.LocalDateSpecifiedUtcKind(), type, responseStatus));

        _logger.LogError("Grpc server error {@data}", new
        {
            responseStatus.ResponseCode,
            responseStatus.ResponseMessage
        });
    }
}
