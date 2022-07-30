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

    public class StopAtr : IStrategy
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        public StopAtr(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            _operationSettings.Value.Strategy.StopAtr!.LookbackPeriods;

        public virtual decimal SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var lastStopAtr = LastStopAtr(quotes);
            return lastStopAtr.LowerBand is not null ? -strategy.Volume : strategy.Volume;
        }

        protected VolatilityStopResult LastStopAtr(IEnumerable<CustomQuote> quotes)
        {
            var atr = _operationSettings.Value.Strategy.StopAtr!;
            var stopAtrs = quotes.GetVolatilityStop(atr.LookbackPeriods, atr.Multiplier);
            return stopAtrs.Last();
        }
    }

    public class StopAtrFollowTrend : StopAtr
    {
        public StopAtrFollowTrend(IOptions<OperationSettings> operationSettings) : base(operationSettings)
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
            var slopes = quotes.GetSlope(linearRegression.LookbackPeriods);
            return slopes.Last();
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
            var fastRsis = quotes.GetRsi(strategy.DoubleRsi!.FastLookbackPeriods);
            var slowRsis = quotes.GetRsi(strategy.DoubleRsi!.SlowLookbackPeriods);
            return fastRsis.Last().Rsi > slowRsis.Last().Rsi;
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
            _operationSettings.Value.Strategy.Macd!.SlowPeriods;

        public virtual decimal SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            return MacdIsGreaterThanSignal(quotes) ? -strategy.Volume : strategy.Volume;
        }

        protected bool MacdIsGreaterThanSignal(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Macd!;
            var macds = quotes.GetMacd(settings.FastPeriods, settings.SlowPeriods, settings.SignalPeriods);
            var lastMacd = macds.Last();
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

    public class SuperTrend : IStrategy
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        public SuperTrend(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            _operationSettings.Value.Strategy.SuperTrend!.LookbackPeriods;

        public virtual decimal SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            return SuperTrendHasUpperBand(quotes) ? -strategy.Volume : strategy.Volume;
        }

        protected bool SuperTrendHasUpperBand(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.SuperTrend!;
            var superTrends = quotes.GetSuperTrend(settings.LookbackPeriods, settings.Multiplier);
            var superTrend = superTrends.Last();
            return superTrend.UpperBand is not null;
        }
    }

    public class SuperTrendFollowTrend : SuperTrend
    {
        public SuperTrendFollowTrend(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override decimal SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            return !SuperTrendHasUpperBand(quotes) ? -strategy.Volume : strategy.Volume;
        }
    }

    public class Crazy : IStrategy
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        public Crazy(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods => 5;

        public virtual decimal SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;

            var up = true;
            var down = true;
            var count = LookbackPeriods;

            CustomQuote? last = null;

            foreach (var quote in quotes.Reverse())
            {
                count--;

                if (last is null)
                {
                    last = quote;
                    continue;
                }

                if (quote.Open < last.Open)
                    up = false;

                if (quote.Open > last.Open)
                    down = false;

                last = quote;

                if (count == 0)
                    break;
            }

            if (up)
                return -strategy.Volume;

            if (down)
                return strategy.Volume;

            return 0;
        }
    }
}
