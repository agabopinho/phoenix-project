using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public interface ILoopService
    {
        Task RunAsync(CancellationToken cancellationToken);
    }

    public static class Operation
    {
        public static readonly string Symbol = "WINQ22";
        public static readonly DateOnly Date = new(2022, 6, 27);
        public static readonly int ChunkSize = 5000;
    }

    public class LoopService : ILoopService
    {
        public static readonly TimeSpan Timeframe = TimeSpan.FromSeconds(10);

        private readonly IRatesStateService _ratesStateService;
        private readonly ILogger<ILoopService> _logger;

        public LoopService(IRatesStateService ratesStateService, ILogger<ILoopService> logger)
        {
            _ratesStateService = ratesStateService;
            _logger = logger;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            await _ratesStateService.CheckNewRatesAsync(
                Operation.Symbol, Operation.Date, Timeframe,
                Operation.ChunkSize, cancellationToken);

            var rates = await _ratesStateService.GetRatesAsync(
                Operation.Symbol, Operation.Date, Timeframe,
                TimeSpan.FromMinutes(15), cancellationToken);

            foreach (var r in rates)
            {
            }
        }
    }
}
