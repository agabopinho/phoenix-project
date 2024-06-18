using Application.Models;
using Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services.Strategy;

public class CancelBuyLimitLoopService(
    State state,
    IOptionsMonitor<OperationOptions> operationSettings,
    ILogger<CancelBuyLimitLoopService> logger
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
