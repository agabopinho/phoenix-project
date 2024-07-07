using Application.Models;
using Application.Options;
using Application.Services.Providers;
using Application.Services.Providers.Range;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Application.Services.Strategies;

public abstract class StrategyLoopService(
    State state,
    IOrderWrapper orderWrapper,
    IOptionsMonitor<OperationOptions> operationSettings,
    ILogger<StrategyLoopService> logger
) : ILoopService
{
    protected State State { get; } = state;
    public IOrderWrapper OrderWrapper { get; } = orderWrapper;
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

    protected bool SignalBuy()
    {
        if (!State.Charts.TryGetValue(MarketDataLoopService.BRICKS_KEY, out var chart))
        {
            return false;
        }

        var bricks = chart.GetUniqueBricks();

        if (bricks.Count < 3)
        {
            return false;
        }

        var index1 = bricks.ElementAt(^1);
        var index2 = bricks.ElementAt(^2);
        var index3 = bricks.ElementAt(^3);

        return SignalBuy(index1, index2, index3);
    }

    protected bool SignalSell()
    {
        if (!State.Charts.TryGetValue(MarketDataLoopService.BRICKS_KEY, out var chart))
        {
            return false;
        }

        var bricks = chart.GetUniqueBricks();

        if (bricks.Count < 3)
        {
            return false;
        }

        var index1 = bricks.ElementAt(^1);
        var index2 = bricks.ElementAt(^2);
        var index3 = bricks.ElementAt(^3);

        return SignalSell(index1, index2, index3);
    }

    protected async Task BuyAsync(double volume, CancellationToken cancellationToken, PositionType? awaitPositionType = PositionType.Buy)
    {
        if (awaitPositionType is not (PositionType.Buy or null))
        {
            throw new InvalidOperationException();
        }

        var deal = await OrderWrapper.BuyAsync(State.LastTick!.Ask!.Value, volume, cancellationToken);

        if (deal > 0)
        {
            await WaitPositionAsync(awaitPositionType, cancellationToken);
        }
    }

    protected async Task SellAsync(double volume, CancellationToken cancellationToken, PositionType? awaitPositionType = PositionType.Sell)
    {
        if (awaitPositionType is not (PositionType.Sell or null))
        {
            throw new InvalidOperationException();
        }

        var deal = await OrderWrapper.SellAsync(State.LastTick!.Bid!.Value, volume, cancellationToken);

        if (deal > 0)
        {
            await WaitPositionAsync(awaitPositionType, cancellationToken);
        }
    }

    protected bool LossOrProfit(Position position)
    {
        if (OperationSettings.CurrentValue.Order.StopLossPips is null &&
            OperationSettings.CurrentValue.Order.TakeProfitPips is null)
        {
            return false;
        }

        var profitPips = position.Type == PositionType.Sell ?
            position.PriceOpen - State.LastTick!.Ask!.Value :
            State.LastTick!.Bid!.Value - position.PriceOpen;

        return
            profitPips >= OperationSettings.CurrentValue.Order.TakeProfitPips ||
            profitPips <= -OperationSettings.CurrentValue.Order.StopLossPips;
    }

    protected async Task WaitPositionAsync(PositionType? positionType, CancellationToken cancellationToken)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        while (State.Position?.Type != positionType && stopwatch.ElapsedMilliseconds < OperationSettings.CurrentValue.Order.AwaitTimeout)
        {
            await Task.Delay(OperationSettings.CurrentValue.Order.WhileDelay, cancellationToken);
        }
    }

    private bool SignalBuy(Brick index1, Brick index2, Brick index3)
    {
        if (!(State.LastTick!.Ask!.Value <= index1.LineDown))
        {
            return false;
        }

        return index1.LineUp < index2.LineUp && index2.LineUp > index3.LineUp;
    }

    private bool SignalSell(Brick index1, Brick index2, Brick index3)
    {
        if (!(State.LastTick!.Bid!.Value >= index1.LineUp))
        {
            return false;
        }

        return index1.LineUp > index2.LineUp && index2.LineUp < index3.LineUp;
    }
}
