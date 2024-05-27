using Application.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Workers;

public class WorkerService(
    ILoopService loopService,
    ILogger<WorkerService> logger) : BackgroundService
{
    private readonly ILoopService _loopService = loopService;
    private readonly ILogger<WorkerService> _logger = logger;
    private readonly Stopwatch _stopwatch = new();

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _stopwatch.Restart();
                await _loopService.RunAsync(stoppingToken);
                _logger.LogDebug("Run in {@ms}ms", _stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception e)
            {
                _logger.LogError(e, "Loop execution error.");
            }
        }
    }
}
