using Application.Helpers;
using Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;

namespace Application.Services.Strategies
{
    public class Renko : IStrategy
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptions<OperationSettings> _operationSettings;
        
        private double _lastRenkoOpen = 0;

        public Renko(IServiceProvider serviceProvider, IOptions<OperationSettings> operationSettings)
        {
            _serviceProvider = serviceProvider;
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
           StrategyFactory.Get(_operationSettings.Value.Strategy.Renko.Use)!.LookbackPeriods;

        private IStrategyFactory StrategyFactory
           => _serviceProvider.GetRequiredService<IStrategyFactory>();

        public virtual double SignalVolume(IEnumerable<IQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Renko;

            var renkos = quotes
                .GetRenko(Convert.ToDecimal(settings.BrickSize), EndType.HighLow)
                .ToArray();

            if (!renkos.Any())
                return 0d;

            var lastRenkoOpen = Convert.ToDouble(renkos.Last().Open);
            if (_lastRenkoOpen == lastRenkoOpen)
                return 0d;
            _lastRenkoOpen = lastRenkoOpen;

            return StrategyFactory
                .Get(settings.Use)!
                .SignalVolume(renkos);
        }
    }
}
