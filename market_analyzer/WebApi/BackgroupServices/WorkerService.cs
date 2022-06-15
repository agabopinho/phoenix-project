using Application.Services;

namespace WebApi.BackgroupServices
{
    public class WorkerService : BackgroundService
    {
        private readonly IRatesService _ratesService;
        private readonly ILogger<WorkerService> _logger;

        public WorkerService(IRatesService ratesService, ILogger<WorkerService> logger)
        {
            _ratesService = ratesService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await InternalExecuteAsync(stoppingToken);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Execute error.");
                }
            }
        }

        private async Task InternalExecuteAsync(CancellationToken stoppingToken)
        {
            var tasks = new List<Task<RatesResult>>();

            foreach (var symbol in new[] { "PETR4", "WINQ22", "WDON22", "CMIG4" })
                tasks.Add(_ratesService.GetRatesAsync(symbol));

            await Task.WhenAll(tasks);

            foreach (var task in tasks)
            {
                var result = task.Result;
                var firstRate = result.Rates!.First();

                _logger.LogInformation("{@data}", new
                {
                    result.Symbol,
                    Timeframe = result.Metadata!.AvailableRatesTimeframes!.First(),
                    firstRate.Time,
                    firstRate.Close
                });
            }
        }
    }
}
