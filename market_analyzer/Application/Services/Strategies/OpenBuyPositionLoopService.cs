using Application.Models;
using Application.Options;
using Application.Services.Providers;
using Grpc.Terminal.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services.Strategies;

public class OpenBuyPositionLoopService(
    State state,
    OrderWrapper orderWrapper,
    IOptionsMonitor<OperationOptions> operationSettings,
    ILogger<OpenBuyPositionLoopService> logger
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

        if (position is not null)
        {
            return;
        }

        if (!State.Charts.TryGetValue(MarketDataLoopService.FAST_BRICKS_KEY, out var fastChart))
        {
            return;
        }

        var fastBricks = fastChart.GetUniqueBricks();

        if (fastBricks.Count < 1)
        {
            return;
        }

        var fastIndex1 = fastBricks.ElementAt(^1);

        if (!BuySignal(fastIndex1))
        {
            return;
        }

        var options = OperationSettings.CurrentValue;
        var lot = options.Order.Lot;

        var deal = await OrderWrapper.BuyAsync(State.LastTick!.Ask!.Value, lot, cancellationToken);

        if (deal > 0)
        {
            await AwaitPositionAsync(PositionType.Buy, cancellationToken);
        }
    }
}
