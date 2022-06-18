using Application.Objects;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class StateService : IStateService
    {
        private readonly ILogger<StateService> _logger;

        public StateService(ILogger<StateService> logger)
        {
            _logger = logger;
        }

        public StateManager CreateStateManager(MarketDataResult marketDataResult)
        {
            var stateManager = new StateManager();
            var state = GetState(marketDataResult);
            stateManager.Update(state);
            return stateManager;
        }

        public void UpdateState(StateManager stateManager, MarketDataResult marketDataResult)
        {
            var state = GetState(marketDataResult);
            
            if (stateManager.States.ContainsKey(state.UpdateAt))
            {
                _logger.LogInformation("State already exists.");

                return;
            }

            stateManager.Update(state);
        }

        private static State GetState(MarketDataResult marketDataResult)
            => new(marketDataResult.RatesInfo!.UpdatedAt, marketDataResult.Rates!.First().Close);
    }
}
