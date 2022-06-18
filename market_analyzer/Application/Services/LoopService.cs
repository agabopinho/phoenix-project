using Application.Objects;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class LoopService : ILoopService
    {
        private static readonly string SYMBOL = "WINQ22";

        private readonly IRatesService _ratesService;
        private readonly IStateService _stateService;
        private readonly ILogger<LoopService> _logger;

        private StateManager? StateManager = null;

        public LoopService(IRatesService ratesService, IStateService stateService, ILogger<LoopService> logger)
        {
            _ratesService = ratesService;
            _stateService = stateService;
            _logger = logger;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var marketDataResult = await _ratesService.GetRatesAsync(SYMBOL, cancellationToken);

            if (!marketDataResult.HasResult)
            {
                _logger.LogWarning("Market data did not produce result.");

                return;
            }

            if (StateManager is null)
            {
                StateManager = _stateService.CreateStateManager(marketDataResult);

                return;
            }

            _stateService.UpdateState(StateManager, marketDataResult);

            Print(StateManager);

            return;
        }

        public void Print(StateManager state)
            => _logger.LogInformation("{@data}", new
            {
                LastUpdateAt = state.States.Last().Value.UpdateAt,
                state.CountUp,
                state.CountDown,
                state.Absolute,
                state.Side
            });
    }
}
