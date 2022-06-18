using Application.Constants;
using Application.Objects;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class LoopService : ILoopService
    {
        private readonly IRatesService _ratesService;
        private readonly IStateService _stateService;
        private readonly ILogger<LoopService> _logger;

        private IStateManager? StateManager = null;

        public LoopService(IRatesService ratesService, IStateService stateService, ILogger<LoopService> logger)
        {
            _ratesService = ratesService;
            _stateService = stateService;
            _logger = logger;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var marketDataResult = await _ratesService.GetRatesAsync(Defaults.Symbol, cancellationToken);

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

            _logger.LogInformation("{@data}", StateManager.PrintInformation());

            return;
        }
    }
}
