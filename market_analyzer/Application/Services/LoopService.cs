using Application.Options;
using Application.Range;
using Application.Services.Providers.Date;
using Grpc.Core;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services;

public class LoopService(
    IMarketDataWrapper marketDataWrapper,
    IOrderManagementSystemWrapper orderManagementSystemWrapper,
    IDateProvider dateProvider,
    IOptionsMonitor<OperationSettings> operationSettings,
    ILogger<ILoopService> logger) : ILoopService
{
    private const int AHEAD_SECONDS = 30;

    private readonly RangeCalculation _rangeCalculation = new(operationSettings.CurrentValue.BrickSize!.Value);
    private readonly List<TerminalError> _errors = [];

    private DateTime _currentTime;
    private Trade? _lastTrade;
    private int _previousBricksCount;
    private int _newBricks;

    private void PreExecution()
    {
        _currentTime = dateProvider.LocalDateSpecifiedUtcKind();
        _errors.Clear();
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        PreExecution();

        await CheckNewPrice(cancellationToken);

        if (_newBricks > 0)
        {
            logger.LogInformation("NewBricks: {newBricks}", _newBricks);
        }

        var position = await GetPositionAsync(cancellationToken);
    }

    private async Task CheckNewPrice(CancellationToken cancellationToken)
    {
        _previousBricksCount = _rangeCalculation.Bricks.Count;

        var fromDate = _lastTrade?.Time.ToDateTime() ?? (_currentTime - _currentTime.TimeOfDay);
        var toDate = _currentTime.AddSeconds(AHEAD_SECONDS);

        if (_previousBricksCount == 0)
        {
            logger.LogInformation("Loading data from: {fromDate}", fromDate);
        }

        var ticksReply = marketDataWrapper.StreamTicksRange(
            operationSettings.CurrentValue.Symbol!,
            fromDate,
            toDate,
            CopyTicks.Trade,
            operationSettings.CurrentValue.StreamingData.ChunkSize, cancellationToken);

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
        var orders = await orderManagementSystemWrapper.GetOrdersAsync(
            group: operationSettings.CurrentValue.Symbol,
            cancellationToken: cancellationToken);

        CheckResponseStatus(ResponseType.GetOrder, orders.ResponseStatus);

        return orders.Orders ?? [];
    }

    private async Task<Position?> GetPositionAsync(CancellationToken cancellationToken)
    {
        var positions = await orderManagementSystemWrapper.GetPositionsAsync(
            group: operationSettings.CurrentValue.Symbol!,
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

        _errors.Add(new(dateProvider.LocalDateSpecifiedUtcKind(), type, responseStatus));

        logger.LogError("Grpc server error {@data}", new
        {
            responseStatus.ResponseCode,
            responseStatus.ResponseMessage
        });
    }
}
