using Application.Options;
using Application.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.BackgroupServices
{
    public class WorkerService : BackgroundService
    {
        private readonly ILoopService _loopService;
        private readonly IOptionsSnapshot<OperationSettings> _operationSettings;
        private readonly ILogger<WorkerService> _logger;

        public WorkerService(
            ILoopService loopService,
            IOptionsSnapshot<OperationSettings> operationSettings,
            ILogger<WorkerService> logger)
        {
            _loopService = loopService;
            _operationSettings = operationSettings;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _loopService.RunAsync(_operationSettings.Value, stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Loop execution error.");
                }
            }
        }
    }
}
