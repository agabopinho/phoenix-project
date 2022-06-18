using Application.Objects;

namespace Application.Services
{
    public interface IStateService
    {
        IStateManager CreateStateManager(MarketDataResult marketDataResult);
        void UpdateState(IStateManager stateManager, MarketDataResult marketDataResult);
    }
}
