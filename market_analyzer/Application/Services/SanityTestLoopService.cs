using Application.Models;
using Application.Options;
using Application.Services.Providers;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Application.Services;

public class SanityTestLoopService(
    OrderWrapper orderWrapper,
    State state,
    IOptionsMonitor<OperationOptions> operationSettings,
    ILogger<SanityTestLoopService> logger
) : ILoopService
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

        if (state.Delayed)
        {
            return;
        }

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        try
        {
            await CancelTestOrdersAsync(cancellationToken);

            var buyTest = RunTestAsync(OrderType.BuyLimit, cancellationToken);
            var sellTest = RunTestAsync(OrderType.SellLimit, cancellationToken);

            await Task.WhenAll(buyTest, sellTest);

            if (buyTest.Result && sellTest.Result)
            {
                logger.LogInformation("Passed the sanity test in {ms}ms.", stopwatch.ElapsedMilliseconds);

                state.SetSanityTestStatus(SanityTestStatus.Passed);
            }
            else
            {
                logger.LogError("Failed the sanity test in {ms}ms.", stopwatch.ElapsedMilliseconds);

                state.SetSanityTestStatus(SanityTestStatus.Error);
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error when running sanity test in {ms}.", stopwatch.ElapsedMilliseconds);

            state.SetSanityTestStatus(SanityTestStatus.Error);
        }
    }

    private async Task<bool> RunTestAsync(OrderType orderType, CancellationToken cancellationToken)
    {
        if (orderType is not (OrderType.BuyLimit or OrderType.SellLimit))
        {
            throw new InvalidOperationException("Invalid order type.");
        }

        var testResult = true;
        var sanityTestOptions = operationSettings.CurrentValue.SanityTest;

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        var limitPrice = orderType is OrderType.BuyLimit ?
            state.LastTick!.Bid!.Value - sanityTestOptions.PipsRange :
            state.LastTick!.Ask!.Value + sanityTestOptions.PipsRange;

        var ticket = orderType is OrderType.BuyLimit ?
            await orderWrapper.BuyLimitAsync(limitPrice, sanityTestOptions.Lot, sanityTestOptions.Magic, cancellationToken) :
            await orderWrapper.SellLimitAsync(limitPrice, sanityTestOptions.Lot, sanityTestOptions.Magic, cancellationToken);

        logger.LogInformation("{orderType} order launched in {ms}ms.", orderType, stopwatch.ElapsedMilliseconds);
        stopwatch.Restart();

        var orderLimit = default(Order);

        while ((orderLimit = state.Orders.FirstOrDefault(it => it.Ticket == ticket)) is null)
        {
            await Task.Delay(operationSettings.CurrentValue.Order.WhileDelay, cancellationToken);
        }

        logger.LogInformation("Waited {orderType} order appear in the order list in {ms}ms.", orderType, stopwatch.ElapsedMilliseconds);
        stopwatch.Restart();

        for (var i = 0; i < sanityTestOptions.OrderModifications && testResult; i++)
        {
            limitPrice = orderType is OrderType.BuyLimit ?
                limitPrice - sanityTestOptions.PipsStep :
                limitPrice + sanityTestOptions.PipsStep;

            await orderWrapper.ModifyOrderLimitAsync(ticket.Value, limitPrice, cancellationToken);

            while ((orderLimit = state.Orders.FirstOrDefault(it => it.Ticket == ticket)) is not null &&
                orderLimit.PriceOpen != limitPrice)
            {
                await Task.Delay(operationSettings.CurrentValue.Order.WhileDelay, cancellationToken);
            }

            if (orderLimit?.PriceOpen == limitPrice)
            {
                logger.LogInformation("Modified order {orderType} in {ms}ms.", orderType, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                logger.LogError("Failed to find modified {orderType} price in {ms}ms.", orderType, stopwatch.ElapsedMilliseconds);
                testResult = false;
            }

            stopwatch.Restart();
        }

        await orderWrapper.CancelAsync(ticket.Value, cancellationToken);

        while (state.Orders.Any(it => it.Ticket == ticket))
        {
            await Task.Delay(operationSettings.CurrentValue.Order.WhileDelay, cancellationToken);
        }

        logger.LogInformation("Canceled {orderType} order in {ms}ms.", orderType, stopwatch.ElapsedMilliseconds);
        stopwatch.Restart();

        return testResult;
    }

    private async Task CancelTestOrdersAsync(CancellationToken cancellationToken)
    {
        var sanityTestOptions = operationSettings.CurrentValue.SanityTest;

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        foreach (var order in state.Orders.Where(it => it.Magic == sanityTestOptions.Magic))
        {
            await orderWrapper.CancelAsync(order.Ticket!.Value, cancellationToken);
        }

        logger.LogInformation("Canceled all test orders in {ms}ms.", stopwatch.ElapsedMilliseconds);
    }
}
