using Application.Models;
using Application.Options;
using Application.Services.Providers.Range;
using Grpc.Terminal.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Application.Services.Strategies;

public abstract class StrategyLoopService(
    State state,
    IOptionsMonitor<OperationOptions> operationSettings,
    ILogger<StrategyLoopService> logger
) : ILoopService
{
    protected State State { get; } = state;
    protected IOptionsMonitor<OperationOptions> OperationSettings { get; } = operationSettings;
    protected ILogger Logger { get; } = logger;

    public Task<bool> StoppedAsync(CancellationToken stoppingToken)
    {
        return Task.FromResult(false);
    }

    public Task<bool> CanRunAsync(CancellationToken stoppingToken)
    {
        if (OperationSettings.CurrentValue.Order.ProductionMode is ProductionMode.Off)
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
        if (State.BricksUpdatedAt <= DateTime.UnixEpoch)
        {
            Logger.LogInformation("Waiting for bricks to load...");

            while (State.BricksUpdatedAt <= DateTime.UnixEpoch)
            {
                await Task.Delay(OperationSettings.CurrentValue.Order.WhileDelay, cancellationToken);
            }
        }

        await StrategyRunAsync(cancellationToken);
    }

    protected abstract Task StrategyRunAsync(CancellationToken cancellation);

    protected bool BuySignal(Brick fastIndex1)
    {
        return State.LastTick!.Ask <= fastIndex1.LineDown;
    }

    protected bool SellSignal(Brick fastIndex1)
    {
        return State.LastTick!.Bid >= fastIndex1.LineUp;
    }

    protected async Task AwaitPositionAsync(PositionType? positionType, CancellationToken cancellationToken)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        while (State.Position?.Type != positionType && stopwatch.ElapsedMilliseconds < OperationSettings.CurrentValue.Order.AwaitTimeout)
        {
            await Task.Delay(OperationSettings.CurrentValue.Order.WhileDelay, cancellationToken);
        }
    }
}
