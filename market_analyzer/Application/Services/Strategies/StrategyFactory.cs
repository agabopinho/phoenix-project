using Microsoft.Extensions.DependencyInjection;

namespace Application.Services.Strategies
{
    public class StrategyFactory : IStrategyFactory
    {
        private readonly IServiceProvider _serviceProvider;

        public StrategyFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IEnumerable<IStrategy> GetAll()
            => GetStrategies();

        public IStrategy? Get(string name)
            => GetStrategies().FirstOrDefault(it => it.GetType().Name.Equals(name, StringComparison.OrdinalIgnoreCase));

        private IEnumerable<IStrategy> GetStrategies()
            => _serviceProvider.GetRequiredService<IEnumerable<IStrategy>>();
    }
}
