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
        protected readonly IOptions<OperationSettings> _operationSettings;

        public Atr(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            _operationSettings.Value.Strategy.Atr!.LookbackPeriods;

        public virtual decimal SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var lastStopAtr = LastStopAtr(quotes, strategy);
            return lastStopAtr.LowerBand is not null ? -strategy.Volume : strategy.Volume;
        }

        protected VolatilityStopResult LastStopAtr(IEnumerable<CustomQuote> quotes, StrategySettings strategy)
        {
            var atr = strategy.Atr!;
            var stopAtr = quotes.GetVolatilityStop(atr.LookbackPeriods, atr.Multiplier);
            return stopAtr.Last();
        }
    }

    public class AtrFollowTrend : Atr
    {
        public AtrFollowTrend(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override decimal SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var lastStopAtr = LastStopAtr(quotes, strategy);
            return lastStopAtr.UpperBand is not null ? -strategy.Volume : strategy.Volume;
        }
    }

    public class LinearRegression : IStrategy
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        public LinearRegression(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            _operationSettings.Value.Strategy.LinearRegression!.LookbackPeriods;

        public virtual decimal SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var lastSlope = LastSlope(quotes, strategy);
            return lastSlope.Slope > 0 ? -strategy.Volume : strategy.Volume;
        }

        protected SlopeResult LastSlope(IEnumerable<CustomQuote> quotes, StrategySettings strategy)
        {
            var linearRegression = strategy.LinearRegression!;
            var slope = quotes.GetSlope(linearRegression.LookbackPeriods);
            return slope.Last();
        }
    }

    public class LinearRegressionFollowTrend : LinearRegression
    {
        public LinearRegressionFollowTrend(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override decimal SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var lastSlope = LastSlope(quotes, strategy);
            return lastSlope.Slope < 0 ? -strategy.Volume : strategy.Volume;
        }
    }
}
