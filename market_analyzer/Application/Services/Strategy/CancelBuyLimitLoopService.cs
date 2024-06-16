using Application.Models;
using Application.Options;
using Application.Services.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services.Strategy;

public class CancelBuyLimitLoopService(
    State state,
    IOptionsMonitor<OperationOptions> operationSettings,
    OrderWrapper orderWrapper,
    ILogger<CancelBuyLimitLoopService> logger) : StrategyLoopService(state, operationSettings, logger)
{
    protected override async Task StrategyRunAsync(CancellationToken cancellationToken)
    {
        var settings = OperationSettings.CurrentValue;

        if (State.Position is not null)
        {
            return;
        }

        await Task.CompletedTask;
    }
}
