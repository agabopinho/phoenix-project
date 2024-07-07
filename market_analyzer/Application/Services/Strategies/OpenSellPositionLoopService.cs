using Application.Models;
using Application.Options;
using Application.Services.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services.Strategies;

public class OpenSellPositionLoopService(
    State state,
    IOrderWrapper orderWrapper,
    IOptionsMonitor<OperationOptions> operationSettings,
    ILogger<OpenSellPositionLoopService> logger
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

        if (position is not null)
        {
            return;
        }

        if (!SignalSell())
        {
            return;
        }

        var options = OperationSettings.CurrentValue;
        var lot = options.Order.Lot;

        await SellAsync(lot, cancellationToken);
    }
}
