using Application.Models;
using Application.Options;
using Grpc.Terminal;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Options;

namespace Application.Services;

public class PositionLoopService(
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
        var position = await GetPositionAsync(cancellationToken);
        state.SetPosition(position);
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
