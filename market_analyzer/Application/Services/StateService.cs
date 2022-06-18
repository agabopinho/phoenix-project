using Application.Constants;
using Application.Objects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Application.Services
{
    public class StateService : IStateService
    {
        private readonly IServiceProvider _serviceProvider;

        public StateService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IStateManager CreateStateManager(MarketDataResult marketDataResult)
        {
            var logger = _serviceProvider.GetRequiredService<ILogger<StateManager>>();
            var stateManager = new StateManager(Defaults.SlidingTime, logger);
            UpdateState(stateManager, marketDataResult);
            return stateManager;
        }

        public void UpdateState(IStateManager stateManager, MarketDataResult marketDataResult)
        {
            var state = GetState(marketDataResult);
            stateManager.Update(state);
        }

        private static State GetState(MarketDataResult marketDataResult)
            => new(marketDataResult.RatesInfo!.UpdatedAt, marketDataResult.Rates!.First().Close);
    }
}
