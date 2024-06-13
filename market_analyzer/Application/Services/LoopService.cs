using Application.Models;
using Application.Options;
using Application.Services.Providers.Date;
using Grpc.Terminal;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Options;

namespace Application.Services;

public class LoopService(
    IOrderManagementSystemWrapper orderManagementSystemWrapper,
    IDateProvider dateProvider,
    State state,
    IOptionsMonitor<OperationSettings> operationSettings) : ILoopService
{
    private DateTime _currentTime;

    private void PreExecution()
    {
        _currentTime = dateProvider.LocalDateSpecifiedUtcKind();
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        PreExecution();

        var position = GetPositionAsync(cancellationToken);
        var orders = GetOrdersAsync(cancellationToken);

        await Task.WhenAll(position, orders);
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

    private async Task<Position?> GetPositionAsync(CancellationToken cancellationToken)
    {
        var positions = await orderManagementSystemWrapper.GetPositionsAsync(
            symbol: operationSettings.CurrentValue.Symbol!,
            group: null,
            ticket: null,
            cancellationToken: cancellationToken);

        state.CheckResponseStatus(ResponseType.GetPosition, positions.ResponseStatus);

        return positions.Positions?.FirstOrDefault();
    }
}
