using Application.Models;
using Application.Options;
using Application.Services.Providers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services;

public class SanityTestLoopService(
    OrderWrapper orderWrapper,
    State state,
    IOptionsMonitor<OperationOptions> operationSettings,
    ILogger<SanityTestLoopService> logger) : ILoopService
{
    public Task<bool> StoppedAsync(CancellationToken stoppingToken)
    {
        return Task.FromResult(state.SanityTestStatus != SanityTestStatus.WaitExecution);
    }

    public Task<bool> CanRunAsync(CancellationToken stoppingToken)
    {
        if (!state.ReadyForSanityTest)
        {
            if (state.WarnAuction)
            {
                logger.LogWarning("WarnAuction: {LastTick}", state.LastTick);
            }
            else
            {
                logger.LogDebug("Waiting ready.");
            }

            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
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
