using Application.Models;
using Application.Options;
using Application.Services.Providers;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Application.Services.Strategy;

public abstract class StrategyLoopService(
    State state,
    OrderWrapper orderWrapper,
    IOptionsMonitor<OperationOptions> operationSettings,
    ILogger logger
) : ILoopService
{
    protected State State { get; } = state;
    protected OrderWrapper OrderWrapper { get; } = orderWrapper;
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
        if (State.BricksUpdated <= DateTime.UnixEpoch)
        {
            Logger.LogInformation("Waiting for bricks to load...");

            while (State.BricksUpdated <= DateTime.UnixEpoch)
            {
                await Task.Delay(OperationSettings.CurrentValue.Order.WhileDelay, cancellationToken);
            }
        }

        await StrategyRunAsync(cancellationToken);
    }

    protected abstract Task StrategyRunAsync(CancellationToken cancellationToken);

    protected bool PermittedDistance(OrderType orderType, double price, double? proximity = null)
    {
        if (orderType is not (OrderType.BuyLimit or OrderType.SellLimit))
        {
            throw new InvalidOperationException("Invalid order type.");
        }

        var delta = orderType is OrderType.BuyLimit ?
            State.LastTick!.Last!.Value - price :
            price - State.LastTick!.Last!.Value;

        return delta >= (proximity ?? OperationSettings.CurrentValue.Order.MaximumPriceProximity);
    }

    protected async Task<Order?> AwaitOrderAsync(long ticket, CancellationToken cancellationToken)
    {
        Order? order;

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        while ((order = State.Orders.FirstOrDefault(it => it.Ticket == ticket)) is null)
        {
            if (stopwatch.ElapsedMilliseconds >= OperationSettings.CurrentValue.Order.WaitingTimeout)
            {
                Logger.LogWarning("Timeout when waiting for order {ticket}", ticket);

                break;
            }

            await Task.Delay(OperationSettings.CurrentValue.Order.WhileDelay, cancellationToken);
        }

        return order;
    }

    protected async Task<Order?> AwaitOrderPriceAsync(long ticket, double price, CancellationToken cancellationToken)
    {
        Order? order;

        var stopwatch = new Stopwatch();
        stopwatch.Start();

        while ((order = State.Orders.FirstOrDefault(it => it.Ticket == ticket && it.PriceOpen == price)) is null)
        {
            if (stopwatch.ElapsedMilliseconds >= OperationSettings.CurrentValue.Order.WaitingTimeout)
            {
                Logger.LogWarning("Timeout when waiting for order {ticket}", ticket);

                break;
            }

            await Task.Delay(OperationSettings.CurrentValue.Order.WhileDelay, cancellationToken);
        }

        return order;
    }

    protected async Task NewOrderAsync(OrderType orderType, double price, double lot, IEnumerable<Order> cancelTickets, CancellationToken cancellationToken)
    {
        if (orderType is not (OrderType.BuyLimit or OrderType.SellLimit))
        {
            throw new InvalidOperationException("Invalid order type.");
        }

        if (!PermittedDistance(orderType, price))
        {
            return;
        }

        var taskList = Cancel(cancelTickets, cancellationToken);
        var orderTask = orderType is OrderType.BuyLimit ?
            OrderWrapper.BuyLimitAsync(price, lot, cancellationToken) :
            OrderWrapper.SellLimitAsync(price, lot, cancellationToken);

        taskList.Add(orderTask);

        await Task.WhenAll(taskList);

        if (orderTask.Result is not null)
        {
            await AwaitOrderAsync(orderTask.Result.Value, cancellationToken);
        }
    }

    protected async Task ModifyOrderAsync(OrderType orderType, Order modifyTicket, double price, double lot, IEnumerable<Order> cancelTickets, CancellationToken cancellationToken)
    {
        if (orderType is not (OrderType.BuyLimit or OrderType.SellLimit))
        {
            throw new InvalidOperationException("Invalid order type.");
        }

        if (!PermittedDistance(orderType, price))
        {
            return;
        }

        var taskList = Cancel(cancelTickets, cancellationToken);
        var orderTask = OrderWrapper.ModifyOrderLimitAsync(modifyTicket.Ticket!.Value, price, cancellationToken);

        taskList.Add(orderTask);

        await Task.WhenAll(taskList);

        if (orderTask.Result is not null)
        {
            await AwaitOrderPriceAsync(modifyTicket.Ticket!.Value, price, cancellationToken);
        }
    }

    protected List<Task> Cancel(IEnumerable<Order> cancelTickets, CancellationToken cancellationToken)
    {
        var taskList = new List<Task>();

        foreach (var order in cancelTickets)
        {
            taskList.Add(OrderWrapper.CancelAsync(order.Ticket!.Value, cancellationToken));
        }

        return taskList;
    }
}
