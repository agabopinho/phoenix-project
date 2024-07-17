using Application.Models;
using Application.Options;
using Application.Services.Providers;
using Grpc.Terminal.Enums;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services.Strategies;

public class OpenSellPositionLoopService(
    State state,
    IMarketDataWrapper marketData,
    IOrderWrapper order,
    IOptionsMonitor<OperationOptions> operationSettings,
    ILogger<OpenSellPositionLoopService> logger
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

        if (position is not null)
        {
            return;
        }

        if (await SignalAsync(cancellationToken) != PositionType.Sell)
        {
            return;
        }

        var options = OperationSettings.CurrentValue;
        var lot = options.Order.Lot;

        await SellAsync(lot, cancellationToken);
    }
}
