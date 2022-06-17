using Application.Objects;
using Application.Services;

namespace WebApi.BackgroupServices
{
    public class WorkerService : BackgroundService
    {
        public static readonly string SYMBOL = "WINQ22";

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
            var result = await _ratesService.GetRatesAsync(SYMBOL, stoppingToken);

            var firstRate = result.Rates!.First();

            _logger.LogInformation("{@data}", new
            {
                result.Symbol,
                Timeframe = result.Metadata!.AvailableRatesTimeframes!.First(),
                firstRate.Time,
                firstRate.Close,
                result.Metadata.UpdatedAt
            });
        }
    }
}
