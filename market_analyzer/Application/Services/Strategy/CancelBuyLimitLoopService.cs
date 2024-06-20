using Application.Models;
using Application.Options;
using Application.Services.Providers;
using Grpc.Terminal.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services.Strategy;

public class CancelBuyLimitLoopService(
    State state,
    OrderWrapper orderWrapper,
    IOptionsMonitor<OperationOptions> operationSettings,
    ILogger<CancelBuyLimitLoopService> logger
) : StrategyLoopService(state, orderWrapper, operationSettings, logger)
{
    protected override async Task StrategyRunAsync(CancellationToken cancellationToken)
    {
        if (State.Delayed)
        {
            return;
        }

        var position = State.Position;

        if (position?.Type is not PositionType.Buy)
        {
            return;
        }

        var orders = State.Orders
            .Where(it =>
                it.Type == OrderType.BuyLimit &&
                PermittedDistance(OrderType.BuyLimit, it.PriceOpen!.Value) &&
                it.Magic == OperationSettings.CurrentValue.Order.Magic);

        var taskList = Cancel(orders, cancellationToken);

        await Task.WhenAll(taskList);
    }
}
