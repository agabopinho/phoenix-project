namespace Application.Services.Strategies
{
    public interface IStrategyFactory
    {
        IStrategy? Get(string name);
    }
}
