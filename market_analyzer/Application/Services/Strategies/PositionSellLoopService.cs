using Application.Models;
using Application.Options;
using Application.Services.Providers;
using Grpc.Terminal.Enums;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services.Strategies;

public class PositionSellLoopService(
    State state,
    IMarketDataWrapper marketData,
    IOrderWrapper order,
    IOptionsMonitor<OperationOptions> operationSettings,
    ILogger<PositionSellLoopService> logger
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

        if (position?.Type is not PositionType.Sell)
        {
            return;
        }

        var signalBuy = await SignalAsync(cancellationToken) == PositionType.Buy;

        if (!signalBuy && !LossOrProfit(position))
        {
            return;
        }

        var lot = OperationSettings.CurrentValue.Order.Lot;

        await BuyAsync(signalBuy ? lot * 2 : lot, cancellationToken, signalBuy ? PositionType.Buy : null);
    }
}