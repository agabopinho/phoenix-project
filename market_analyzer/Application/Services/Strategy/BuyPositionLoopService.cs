using Application.Models;
using Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services.Strategy;

public class BuyPositionLoopService(
    State state,
    IOptionsMonitor<OperationOptions> operationSettings,
    ILogger<BuyPositionLoopService> logger
) : StrategyLoopService(state, operationSettings, logger)
{
    protected override async Task StrategyRunAsync(CancellationToken cancellationToken)
    {
        if (State.Position is not null)
        {
            return;
        }

        await Task.CompletedTask;
    }
}
