using Application.Models;
using Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Application.Services.Strategy;

public abstract class StrategyLoopService(
    State state,
    IOptionsMonitor<OperationOptions> operationSettings,
    ILogger logger) : ILoopService
{
    private const int WAIT_BRICKS_LOAD_DELAY = 10;

    public State State { get; } = state;
    public IOptionsMonitor<OperationOptions> OperationSettings { get; } = operationSettings;
    public ILogger Logger { get; } = logger;

    public Task<bool> CanRunAsync(CancellationToken stoppingToken)
    {
        return Task.FromResult(true);
    }

    public Task<bool> StoppedAsync(CancellationToken stoppingToken)
    {
        return Task.FromResult(false);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var settings = OperationSettings.CurrentValue;

        if (settings.ProductionMode != ProductionMode.On)
        {
            return;
        }

        if (!State.Ready)
        {
            if (State.WarnAuction)
            {
                Logger.LogWarning("WarnAuction: {WarnAuction} LastTick: {LastTick}", State.WarnAuction, State.LastTick);
            }

            return;
        }

        if (State.BricksUpdated <= DateTime.UnixEpoch)
        {
            await WaitBricksLoadAsync(cancellationToken);
        }

        if (State.SanityTestStatus is
            not SanityTestStatus.Passed and
            not SanityTestStatus.Skipped)
        {
            return;
        }

        await StrategyRunAsync(cancellationToken);
    }

    protected abstract Task StrategyRunAsync(CancellationToken cancellationToken);

    private async Task WaitBricksLoadAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("Waiting for bricks to load...");

        var bricksLoadTime = new Stopwatch();

        bricksLoadTime.Start();

        do
        {
            await Task.Delay(WAIT_BRICKS_LOAD_DELAY, cancellationToken);
        }
        while (State.BricksUpdated <= DateTime.UnixEpoch);

        bricksLoadTime.Stop();

        Logger.LogInformation("Bricks loaded into {ms}ms", bricksLoadTime.ElapsedMilliseconds);
    }
}
