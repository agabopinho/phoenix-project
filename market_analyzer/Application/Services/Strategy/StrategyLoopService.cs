using Application.Models;
using Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services.Strategy;

public abstract class StrategyLoopService(
    State state,
    IOptionsMonitor<OperationOptions> operationSettings,
    ILogger logger
) : ILoopService
{
    private const int WAIT_BRICKS_LOAD_DELAY = 10;

    public State State { get; } = state;
    public IOptionsMonitor<OperationOptions> OperationSettings { get; } = operationSettings;
    public ILogger Logger { get; } = logger;

    public Task<bool> StoppedAsync(CancellationToken stoppingToken)
    {
        return Task.FromResult(false);
    }

    public Task<bool> CanRunAsync(CancellationToken stoppingToken)
    {
        if (OperationSettings.CurrentValue.ProductionMode is ProductionMode.Off)
        {
            return Task.FromResult(false);
        }

        if (!State.ReadyForTrading)
        {
            if (State.WarnAuction)
            {
                Logger.LogWarning("WarnAuction: {LastTick}", State.LastTick);
            }
            else
            {
                Logger.LogDebug("Waiting ready.");
            }

            return Task.FromResult(false);
        }

        return Task.FromResult(true);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        if (State.BricksUpdated <= DateTime.UnixEpoch)
        {
            Logger.LogInformation("Waiting for bricks to load...");

            while (State.BricksUpdated <= DateTime.UnixEpoch)
            {
                await Task.Delay(WAIT_BRICKS_LOAD_DELAY, cancellationToken);
            }
        }

        await StrategyRunAsync(cancellationToken);
    }

    protected abstract Task StrategyRunAsync(CancellationToken cancellationToken);
}
