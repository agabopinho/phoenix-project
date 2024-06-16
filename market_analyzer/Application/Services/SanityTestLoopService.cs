using Application.Models;
using Application.Options;
using Application.Services.Providers;
using Application.Services.Strategy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services;

public class SanityTestLoopService(
    OrderWrapper orderWrapper,
    State state,
    IOptionsMonitor<OperationOptions> operationSettings,
    ILogger<StrategyLoopService> logger) : ILoopService
{
    public Task<bool> CanRunAsync(CancellationToken stoppingToken)
    {
        return Task.FromResult(true);
    }

    public Task<bool> StoppedAsync(CancellationToken stoppingToken)
    {
        return Task.FromResult(state.SanityTestStatus != SanityTestStatus.WaitExecution);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        await Task.CompletedTask;

        if (state.WarnAuction)
        {
            logger.LogWarning("WarnAuction: {WarnAuction} LastTick: {LastTick}", state.WarnAuction, state.LastTick);
        }

        if (!state.Ready)
        {
            return;
        }

        var sanityTestOptions = operationSettings.CurrentValue.SanityTest;

        if (!sanityTestOptions.Execute)
        {
            state.SetSanityTestStatus(SanityTestStatus.Skipped);

            return;
        }

        state.SetSanityTestStatus(SanityTestStatus.Passed);

        await Task.CompletedTask;
    }
}
