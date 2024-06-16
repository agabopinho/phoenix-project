using Application.Models;
using Application.Options;
using Grpc.Terminal;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Options;

namespace Application.Services;

public class OrdersLoopService(
    IOrderManagementSystemWrapper orderManagementSystemWrapper,
    State state,
    IOptionsMonitor<OperationOptions> operationSettings) : ILoopService
{
    public Task<bool> StoppedAsync(CancellationToken stoppingToken)
    {
        return Task.FromResult(false);
    }

    public Task<bool> CanRunAsync(CancellationToken stoppingToken)
    {
        return Task.FromResult(true);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        var orders = await GetOrdersAsync(cancellationToken);

        state.SetOrders([.. orders.ToArray()]);
    }

    private async Task<IEnumerable<Order>> GetOrdersAsync(CancellationToken cancellationToken)
    {
        var orders = await orderManagementSystemWrapper.GetOrdersAsync(
            symbol: operationSettings.CurrentValue.Symbol,
            group: null,
            ticket: null,
            cancellationToken: cancellationToken);

        state.CheckResponseStatus(ResponseType.GetOrder, orders.ResponseStatus);

        return orders.Orders ?? [];
    }
}
