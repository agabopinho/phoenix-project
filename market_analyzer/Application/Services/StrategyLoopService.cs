using Application.Models;
using Application.Options;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services;

public class StrategyLoopService(
    IOrderManagementSystemWrapper orderManagementSystemWrapper,
    State state,
    IOptionsMonitor<OperationSettings> operationSettings,
    ILogger<StrategyLoopService> logger) : ILoopService
{
    public Task RunAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("WarnAuction: {WarnAuction} LastTick: {LastTick}", state.WarnAuction, state.LastTick);
        return Task.CompletedTask;
    }
}
