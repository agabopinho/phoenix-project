using Application.Models;
using Application.Options;
using Grpc.Terminal;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Options;

namespace Application.Services;

public class LastTickLoopService(
    IMarketDataWrapper marketDataWrapper,
    State state,
    IOptionsMonitor<OperationOptions> operationSettings) : ILoopService
{
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
        var symbolTick = await GetSymbolTickAsync(cancellationToken);
        state.SetLastTick(symbolTick);
    }

    private async Task<Tick> GetSymbolTickAsync(CancellationToken cancellationToken)
    {
        var getSymbolTick = await marketDataWrapper.GetSymbolTickAsync(
            symbol: operationSettings.CurrentValue.Symbol!,
            cancellationToken: cancellationToken);

        state.CheckResponseStatus(ResponseType.GetPosition, getSymbolTick.ResponseStatus);

        return getSymbolTick.Tick;
    }
}
