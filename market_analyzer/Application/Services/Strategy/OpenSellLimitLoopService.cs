using Application.Models;
using Application.Options;
using Application.Services.Providers;
using Grpc.Terminal.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services.Strategy;

public class OpenSellLimitLoopService(
    State state,
    OrderWrapper orderWrapper,
    IOptionsMonitor<OperationOptions> operationSettings,
    ILogger<OpenSellLimitLoopService> logger
) : StrategyLoopService(state, orderWrapper, operationSettings, logger)
{
    protected override async Task StrategyRunAsync(CancellationToken cancellationToken)
    {
        if (State.Delayed)
        {
            return;
        }

        if (State.CheckDelayed(State.BricksUpdated))
        {
            return;
        }

        var position = State.Position;

        if (position is not null)
        {
            return;
        }

        var options = OperationSettings.CurrentValue;
        var price = State.Bricks.Last().Open + options.BrickSize - options.Order.Offset;
        var lot = options.Order.Lot;

        var orders = State.Orders
            .Where(it =>
                it.Type == OrderType.SellLimit &&
                it.Magic == options.Order.Magic);

        if (orders.Any())
        {
            if (orders.All(it => it.PriceOpen == price))
            {
                return;
            }

            var averagePrice = orders.Average(it => it.PriceOpen!.Value);

            if (!PermittedDistance(OrderType.SellLimit, averagePrice, options.BrickSize - options.Order.Offset))
            {
                return;
            }
        }

        var modifyTicket = orders
            .FirstOrDefault(it =>
                it.VolumeCurrent == lot &&
                it.PriceOpen != price);

        var cancelTickets = orders
            .Where(it => it.Ticket != modifyTicket?.Ticket);

        if (modifyTicket is null)
        {
            await NewOrderAsync(OrderType.SellLimit, price, lot, cancelTickets, cancellationToken);
        }
        else
        {
            await ModifyOrderAsync(OrderType.SellLimit, modifyTicket, price, lot, cancelTickets, cancellationToken);
        }
    }
}
