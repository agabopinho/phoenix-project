using Application.Models;
using Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services.Strategy;

public class OpenBuyLimitLoopService(
    State state,
    IOptionsMonitor<OperationOptions> operationSettings,
    ILogger<OpenBuyLimitLoopService> logger
) : StrategyLoopService(state, operationSettings, logger)
{
    protected override async Task StrategyRunAsync(CancellationToken cancellationToken)
    {
        if (State.Position is not null)
        {
            return;
        }

        await Task.CompletedTask;

        //var lastBrick = State.Bricks.Last();

        //var brickSize = settings.BrickSize!.Value;
        //var lot = settings.Order.Lot;

        //var sellLimitPrice = lastBrick.Open + brickSize;
        //var buyLimitPrice = lastBrick.Open - brickSize;

        //var modifySellLimitTicket = State.Orders.FirstOrDefault(it => it.Type == OrderType.SellLimit)?.Ticket;
        //var modifyBuyLimitTicket = State.Orders.FirstOrDefault(it => it.Type == OrderType.BuyLimit)?.Ticket;

        //var sellLimit = orderWrapper.SellLimitAsync(sellLimitPrice, lot, modifySellLimitTicket, cancellationToken);
        //var buyLimit = orderWrapper.BuyLimitAsync(buyLimitPrice, lot, modifyBuyLimitTicket, cancellationToken);

        //await Task.WhenAll(sellLimit, buyLimit);
    }
}
