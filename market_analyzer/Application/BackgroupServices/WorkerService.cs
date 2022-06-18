using Application.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Application.BackgroupServices
{
    public class WorkerService : BackgroundService
    {
        private readonly ILoopService _loopService;
        private readonly ILogger<WorkerService> _logger;

        public WorkerService(ILoopService loopService, ILogger<WorkerService> logger)
        {
            _loopService = loopService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await _loopService.RunAsync(stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Loop execution error.");
                }
            }
        }
    }
}
