using Application.Models;
using Application.Options;
using Application.Services.Providers;
using Grpc.Terminal.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services.Strategy;

public class CancelSellLimitLoopService(
    State state,
    OrderWrapper orderWrapper,
    IOptionsMonitor<OperationOptions> operationSettings,
    ILogger<CancelSellLimitLoopService> logger
) : StrategyLoopService(state, orderWrapper, operationSettings, logger)
{
    protected override async Task StrategyRunAsync(CancellationToken cancellationToken)
    {
        if (State.Delayed)
        {
            return;
        }

        if (State.Position?.Type is not PositionType.Sell)
        {
            return;
        }

        var orders = State.Orders
            .Where(it =>
                it.Type == OrderType.SellLimit &&
                PermittedDistance(OrderType.SellLimit, it.PriceOpen!.Value) &&
                it.Magic == OperationSettings.CurrentValue.Order.Magic);

        var taskList = Cancel(orders, cancellationToken);

        await Task.WhenAll(taskList);
    }
}
