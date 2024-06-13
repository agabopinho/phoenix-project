using Application.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Workers;

public class StrategyService(LoopService loopService, ILogger<StrategyService> logger) : BackgroundService
{
    private readonly Stopwatch _stopwatch = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _stopwatch.Restart();
                await loopService.RunAsync(stoppingToken);
                logger.LogDebug("Run in {@ms}ms", _stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception e)
            {
                logger.LogError(e, "Loop execution error.");
            }
        }
    }
}
