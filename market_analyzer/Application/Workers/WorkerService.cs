using Application.Services;
using Application.Services.Providers.Cycle;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Workers
{
    public class WorkerService : BackgroundService
    {
        private readonly ILoopService _loopService;
        private readonly ILogger<WorkerService> _logger;
        private readonly Stopwatch _stopwatch;

        public WorkerService(
            ILoopService loopService,
            ILogger<WorkerService> logger)
        {
            _loopService = loopService;
            _logger = logger;
            _stopwatch = new Stopwatch();
        }

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
                catch (BacktestFinishException)
                {
                    _logger.LogInformation("Backtest finish.");

                    break;
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Loop execution error.");
                }
            }
        }
    }
}
