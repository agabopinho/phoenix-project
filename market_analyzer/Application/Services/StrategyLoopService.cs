using Application.Models;
using Application.Options;
using Application.Services.Providers;
using Grpc.Terminal.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Application.Services;

public class StrategyLoopService(
    OrderWrapper orderWrapper,
    State state,
    IOptionsMonitor<OperationOptions> operationSettings,
    ILogger<StrategyLoopService> logger) : ILoopService
{
    private const int WAIT_BRICKS_LOAD_DELAY = 100;

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
        await Task.CompletedTask;

        if (state.WarnAuction)
        {
            logger.LogWarning("WarnAuction: {WarnAuction} LastTick: {LastTick}", state.WarnAuction, state.LastTick);
        }

        if (!state.Ready)
        {
            return;
        }

        if (state.BricksUpdated is null)
        {
            await WaitBricksLoadAsync(state, logger, cancellationToken);
        }

        if (state.SanityTestStatus is 
            not SanityTestStatus.Passed and 
            not SanityTestStatus.Skipped)
        {
            return;
        }

        if (state.Position is null)
        {
            var lastBrick = state.Bricks.Last();

            var brickSize = operationSettings.CurrentValue.BrickSize!.Value;
            var lot = operationSettings.CurrentValue.Order.Lot;

            var sellLimitPrice = lastBrick.Open + brickSize;
            var buyLimitPrice = lastBrick.Open - brickSize;

            var modifySellLimitTicket = state.Orders.FirstOrDefault(it => it.Type == OrderType.SellLimit)?.Ticket;
            var modifyBuyLimitTicket = state.Orders.FirstOrDefault(it => it.Type == OrderType.BuyLimit)?.Ticket;

            var sellLimit = orderWrapper.SellLimitAsync(sellLimitPrice, lot, modifySellLimitTicket, cancellationToken);
            var buyLimit = orderWrapper.BuyLimitAsync(buyLimitPrice, lot, modifyBuyLimitTicket, cancellationToken);

            await Task.WhenAll(sellLimit, buyLimit);
        }
    }

    private static async Task WaitBricksLoadAsync(State state, ILogger<StrategyLoopService> logger, CancellationToken cancellationToken)
    {
        logger.LogInformation("Waiting for bricks to load...");

        var bricksLoadTime = new Stopwatch();

        bricksLoadTime.Start();

        do
        {
            await Task.Delay(WAIT_BRICKS_LOAD_DELAY, cancellationToken);
        }
        while (state.BricksUpdated is null);

        bricksLoadTime.Stop();

        logger.LogInformation("Bricks loaded into {ms}ms", bricksLoadTime.ElapsedMilliseconds);
    }
}
