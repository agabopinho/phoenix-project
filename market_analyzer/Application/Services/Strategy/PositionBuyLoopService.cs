﻿using Application.Models;
using Application.Options;
using Application.Services.Providers;
using Grpc.Terminal.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services.Strategy;

public class PositionBuyLoopService(
    State state,
    OrderWrapper orderWrapper,
    IOptionsMonitor<OperationOptions> operationSettings,
    ILogger<PositionBuyLoopService> logger
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

        if (State.Position is null || State.Position.Type != PositionType.Buy)
        {
            return;
        }

        var price = State.Bricks.Last().Open + OperationSettings.CurrentValue.BrickSize;
        var lot = State.Position!.Volume!.Value * 2;

        var orders = State.Orders
            .Where(it =>
                it.Type == OrderType.SellLimit &&
                it.Magic == OperationSettings.CurrentValue.Order.Magic);

        if (orders.All(it => it.PriceOpen == price) && orders.Sum(it => it.VolumeCurrent) == lot)
        {
            return;
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
