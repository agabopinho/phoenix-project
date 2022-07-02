using Application.Options;
using Application.Services;
using Application.Services.Providers.Cycle;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;

namespace Application.BackgroupServices
{
    public class WorkerService : BackgroundService
    {
        private readonly ILoopService _loopService;
        private readonly IOptionsSnapshot<OperationSettings> _operationSettings;
        private readonly ILogger<WorkerService> _logger;
        private readonly Stopwatch _stopwatch;

        public WorkerService(
            ILoopService loopService,
            IOptionsSnapshot<OperationSettings> operationSettings,
            ILogger<WorkerService> logger)
        {
            _loopService = loopService;
            _operationSettings = operationSettings;
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
                    await _loopService.RunAsync(_operationSettings.Value, stoppingToken);
                    _stopwatch.Stop();

                    _logger.LogDebug("Complete loop in {@ms}ms", _stopwatch.Elapsed.TotalMilliseconds);
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
