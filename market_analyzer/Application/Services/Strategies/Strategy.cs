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
            var lastStopAtr = LastStopAtr(quotes);
            return lastStopAtr.LowerBand is not null ? -strategy.Volume : strategy.Volume;
        }

        protected VolatilityStopResult LastStopAtr(IEnumerable<CustomQuote> quotes)
        {
            var atr = _operationSettings.Value.Strategy.Atr!;
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
            var lastStopAtr = LastStopAtr(quotes);
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
            var lastSlope = LastSlope(quotes);
            return lastSlope.Slope > 0 ? -strategy.Volume : strategy.Volume;
        }

        protected SlopeResult LastSlope(IEnumerable<CustomQuote> quotes)
        {
            var linearRegression = _operationSettings.Value.Strategy.LinearRegression!;
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
            var lastSlope = LastSlope(quotes);
            return lastSlope.Slope < 0 ? -strategy.Volume : strategy.Volume;
        }
    }

    public class LastBar : IStrategy
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        public LastBar(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods => 1;

        public virtual decimal SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var lastQuote = quotes.SkipLast(1).Last();
            return lastQuote.Close > lastQuote.Open ? -strategy.Volume : strategy.Volume;
        }
    }

    public class LastBarFollowTrend : IStrategy
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        public LastBarFollowTrend(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods => 1;

        public virtual decimal SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var lastQuote = quotes.SkipLast(1).Last();
            return lastQuote.Close < lastQuote.Open ? -strategy.Volume : strategy.Volume;
        }
    }

    public class DoubleRsi : IStrategy
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        public DoubleRsi(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            _operationSettings.Value.Strategy.DoubleRsi!.SlowLookbackPeriods;

        public virtual decimal SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            return FastIsGreaterThanSlow(quotes) ? -strategy.Volume : strategy.Volume;
        }

        protected bool FastIsGreaterThanSlow(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var fastRsi = quotes.GetRsi(strategy.DoubleRsi!.FastLookbackPeriods);
            var slowRsi = quotes.GetRsi(strategy.DoubleRsi!.SlowLookbackPeriods);
            return fastRsi.Last().Rsi > slowRsi.Last().Rsi;
        }
    }

    public class DoubleRsiFollowTrend : DoubleRsi
    {
        public DoubleRsiFollowTrend(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override decimal SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            return !FastIsGreaterThanSlow(quotes) ? -strategy.Volume : strategy.Volume;
        }
    }

    public class Macd : IStrategy
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        public Macd(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            _operationSettings.Value.Strategy.DoubleRsi!.SlowLookbackPeriods;

        public virtual decimal SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            return MacdIsGreaterThanSignal(quotes) ? -strategy.Volume : strategy.Volume;
        }

        protected bool MacdIsGreaterThanSignal(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Macd!;
            var macd = quotes.GetMacd(settings.FastPeriods, settings.SlowPeriods, settings.SignalPeriods);
            var lastMacd = macd.Last();
            return lastMacd.Macd > lastMacd.Signal;
        }
    }

    public class MacdFollowTrend : Macd
    {
        public MacdFollowTrend(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override decimal SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            return !MacdIsGreaterThanSignal(quotes) ? -strategy.Volume : strategy.Volume;
        }
    }
}
