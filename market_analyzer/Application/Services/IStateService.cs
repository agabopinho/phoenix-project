using Application.Objects;

namespace Application.Services
{
    public interface IStateService
    {
        StateManager CreateStateManager(MarketDataResult marketDataResult);
        void UpdateState(StateManager stateManager, MarketDataResult marketDataResult);
    }
}
