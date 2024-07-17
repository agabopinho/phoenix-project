using Application.Models;
using Application.Options;
using Application.Services.Providers;
using Grpc.Terminal.Enums;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services.Strategies;

public class PositionBuyLoopService(
    State state,
    IMarketDataWrapper marketData,
    IOrderWrapper order,
    IOptionsMonitor<OperationOptions> operationSettings,
    ILogger<PositionBuyLoopService> logger
) : StrategyLoopService(state, marketData, order, operationSettings, logger)
{
    protected override async Task StrategyRunAsync(CancellationToken cancellationToken)
    {
        if (State.Delayed)
        {
            return;
        }

        if (State.CheckDelayed(State.ChartUpdatedAt))
        {
            return;
        }

        var position = State.Position;

        if (position?.Type is not PositionType.Buy)
        {
            return;
        }

        var signalSell = await SignalAsync(cancellationToken) == PositionType.Sell;

        if (!signalSell && !LossOrProfit(position))
        {
            return;
        }

        var lot = OperationSettings.CurrentValue.Order.Lot;

        await SellAsync(signalSell ? lot * 2 : lot, cancellationToken, signalSell ? PositionType.Sell : null);
    }
}
