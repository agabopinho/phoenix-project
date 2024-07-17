using Application.Models;
using Application.Options;
using Application.Services.MarketData;
using Application.Services.Providers;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Application.Services.Strategies;

public abstract class StrategyLoopService(
    State state,
    IMarketDataWrapper marketData,
    IOrderWrapper order,
    IOptionsMonitor<OperationOptions> operationSettings,
    ILogger<StrategyLoopService> logger
) : ILoopService
{
    private int _lastChartCount;

    protected State State { get; } = state;
    protected IMarketDataWrapper MarketData { get; } = marketData;
    protected IOrderWrapper Order { get; } = order;
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
        if (State.ChartUpdatedAt <= DateTime.UnixEpoch)
        {
            Logger.LogInformation("Waiting for chart to load...");

            while (State.ChartUpdatedAt <= DateTime.UnixEpoch)
            {
                await Task.Delay(OperationSettings.CurrentValue.Order.WhileDelay, cancellationToken);
            }
        }

        await StrategyRunAsync(cancellationToken);
    }

    protected abstract Task StrategyRunAsync(CancellationToken cancellation);

    protected async Task<PositionType?> SignalAsync(CancellationToken cancellationToken)
    {
        if (!State.RatesCharts.TryGetValue(RatesMarketDataLoopService.RATES_KEY, out var rates))
        {
            return null;
        }

        if (_lastChartCount == 0)
        {
            _lastChartCount = rates.Count;

            return null;
        }

        if (rates.Count - _lastChartCount == 0)
        {
            return null;
        }

        if (rates.Count < 2)
        {
            return null;
        }

        _lastChartCount = rates.Count;

        var index2 = rates.ElementAt(^2);
        var index3 = rates.ElementAt(^3);

        var pctChange = GetPctChange(index2, index3);

        if (index2.Close > index2.Open)
        {
            var reply = await MarketData.PredictAsync(MarketDataWrapper.BOUGHT_MODEL, pctChange!.Value, cancellationToken);
            return reply.Prediction == 0 ? PositionType.Sell : null;
        }
        else if (index2.Close < index2.Open)
        {
            var reply = await MarketData.PredictAsync(MarketDataWrapper.SOLD_MODEL, pctChange!.Value, cancellationToken);
            return reply.Prediction == 0 ? PositionType.Buy : null;
        }

        return null;
    }

    protected async Task BuyAsync(double volume, CancellationToken cancellationToken, PositionType? waitPositionType = PositionType.Buy)
    {
        if (waitPositionType is not (PositionType.Buy or null))
        {
            throw new InvalidOperationException();
        }

        var deal = await Order.BuyAsync(State.LastTick!.Ask!.Value, volume, cancellationToken);

        if (deal > 0)
        {
            await WaitPositionAsync(waitPositionType, cancellationToken);
        }
    }

    protected async Task SellAsync(double volume, CancellationToken cancellationToken, PositionType? waitPositionType = PositionType.Sell)
    {
        if (waitPositionType is not (PositionType.Sell or null))
        {
            throw new InvalidOperationException();
        }

        var deal = await Order.SellAsync(State.LastTick!.Bid!.Value, volume, cancellationToken);

        if (deal > 0)
        {
            await WaitPositionAsync(waitPositionType, cancellationToken);
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

    private static double? GetPctChange(Rate index2, Rate index3)
    {
        return (index2.Close - index3.Close) / index3.Close;
    }
}
