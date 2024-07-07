using Application.Models;
using Application.Options;
using Application.Services.Providers;
using Grpc.Terminal.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services.Strategies;

public class PositionSellLoopService(
    State state,
    IOrderWrapper orderWrapper,
    IOptionsMonitor<OperationOptions> operationSettings,
    ILogger<PositionSellLoopService> logger
) : StrategyLoopService(state, orderWrapper, operationSettings, logger)
{
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

        if (position?.Type is not PositionType.Sell)
        {
            return;
        }

        var signalBuy = SignalBuy();

        if (!signalBuy && !LossOrProfit(position))
        {
            return;
        }

        var lot = OperationSettings.CurrentValue.Order.Lot;

        await BuyAsync(signalBuy ? lot * 2 : lot, cancellationToken, signalBuy ? PositionType.Buy : null);
    }
}