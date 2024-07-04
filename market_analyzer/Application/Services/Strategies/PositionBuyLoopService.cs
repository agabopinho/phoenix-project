using Application.Models;
using Application.Options;
using Application.Services.Providers;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services.Strategies;

public class PositionBuyLoopService(
    State state,
    OrderWrapper orderWrapper,
    IOptionsMonitor<OperationOptions> operationSettings,
    ILogger<PositionBuyLoopService> logger
) : StrategyLoopService(state, operationSettings, logger)
{
    protected OrderWrapper OrderWrapper { get; } = orderWrapper;

    protected override async Task StrategyRunAsync(CancellationToken cancellationToken)
    {
        if (State.Delayed)
        {
            return;
        }

        if (State.CheckDelayed(State.BricksUpdatedAt))
        {
            return;
        }

        var position = State.Position;

        if (position?.Type is not PositionType.Buy)
        {
            return;
        }

        if (!ChartSignal() && !LossOrProfit(position))
        {
            return;
        }

        var deal = await OrderWrapper.SellAsync(State.LastTick!.Bid!.Value, position.Volume!.Value, cancellationToken);

        if (deal > 0)
        {
            await AwaitPositionAsync(positionType: null, cancellationToken);
        }
    }

    private bool ChartSignal()
    {
        if (!State.Charts.TryGetValue(MarketDataLoopService.FAST_BRICKS_KEY, out var fastChart))
        {
            return false;
        }

        var fastBricks = fastChart.GetUniqueBricks();

        if (fastBricks.Count < 3)
        {
            return false;
        }

        var fastIndex1 = fastBricks.ElementAt(^1);
        var fastIndex2 = fastBricks.ElementAt(^2);
        var fastIndex3 = fastBricks.ElementAt(^3);

        return fastIndex1.LineUp > fastIndex2.LineUp && fastIndex2.LineUp < fastIndex3.LineUp;
    }

    private bool LossOrProfit(Position position)
    {
        var bid = State.LastTick!.Bid!.Value;
        var profitPips = bid - position.PriceOpen;

        return 
            profitPips >= OperationSettings.CurrentValue.Order.TakeProfitPips || 
            profitPips <= -OperationSettings.CurrentValue.Order.StopLossPips;
    }
}
