using Application.Helpers;
using Application.Options;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;

namespace Application.Services.Strategies
{
    public interface IStrategy
    {
        int LookbackPeriods { get; }

        decimal SignalVolume(IEnumerable<CustomQuote> quotes);
    }

    public interface IStrategyFactory
    {
        IStrategy? Get(string name);
    }

    public class StrategyFactory : IStrategyFactory
    {
        private readonly IEnumerable<IStrategy> _strategies;

        public StrategyFactory(IEnumerable<IStrategy> strategies)
        {
            _strategies = strategies;
        }

        public IStrategy? Get(string name)
            => _strategies.FirstOrDefault(it => it.GetType().Name.Equals(name, StringComparison.OrdinalIgnoreCase));
    }

    public class Atr : IStrategy
    {
        private readonly IOptions<OperationSettings> _operationSettings;

        public Atr(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            _operationSettings.Value.Strategy.Atr!.LookbackPeriods;

        public decimal SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var atr = strategy.Atr!;
            var stopAtr = quotes.GetVolatilityStop(atr.LookbackPeriods, atr.Multiplier);
            var lastStopAtr = stopAtr.Last();

            return lastStopAtr.LowerBand is not null ? -strategy.Volume : strategy.Volume;
        }
    }

    public class LinearRegression : IStrategy
    {
        private readonly IOptions<OperationSettings> _operationSettings;

        public LinearRegression(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            _operationSettings.Value.Strategy.LinearRegression!.LookbackPeriods;

        public decimal SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var linearRegression = strategy.LinearRegression!;
            var slope = quotes.GetSlope(linearRegression.LookbackPeriods);
            var lastSlope = slope.Last();

            return lastSlope.Slope < 0 ? -strategy.Volume : strategy.Volume;
        }
    }
}
