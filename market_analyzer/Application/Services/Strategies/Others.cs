using Application.Helpers;
using Application.Options;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;

namespace Application.Services.Strategies
{
    public class VolatilityStop : IStrategy
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        public VolatilityStop(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            _operationSettings.Value.Strategy.VolatilityStop.LookbackPeriods;

        public virtual double SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            return StopAtrHasLowerBand(quotes) ? -strategy.Volume : strategy.Volume;
        }

        protected virtual bool StopAtrHasLowerBand(IEnumerable<CustomQuote> quotes)
        {
            var atr = _operationSettings.Value.Strategy.VolatilityStop;
            var stopAtrs = quotes.GetVolatilityStop(atr.LookbackPeriods, atr.Multiplier);
            var stopAtr = stopAtrs.Last();
            return stopAtr.LowerBand is not null;
        }
    }

    public class VolatilityStopFt : VolatilityStop
    {
        public VolatilityStopFt(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override double SignalVolume(IEnumerable<CustomQuote> quotes)
            => base.SignalVolume(quotes) * -1;
    }

    public class Slope : IStrategy
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        public Slope(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            _operationSettings.Value.Strategy.Slope.LookbackPeriods;

        public virtual double SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            return LastSlopeIsGreaterThanZero(quotes) ? -strategy.Volume : strategy.Volume;
        }

        protected virtual bool LastSlopeIsGreaterThanZero(IEnumerable<CustomQuote> quotes)
        {
            var linearRegression = _operationSettings.Value.Strategy.Slope;
            var slopes = quotes.GetSlope(linearRegression.LookbackPeriods);
            var slope = slopes.Last();
            return slope.Slope > 0;
        }
    }

    public class SlopeFt : Slope
    {
        public SlopeFt(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override double SignalVolume(IEnumerable<CustomQuote> quotes)
            => base.SignalVolume(quotes) * -1;
    }

    public class LastBar : IStrategy
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        public LastBar(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods => 0;

        public virtual double SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            return LastBarIsUp(quotes) ? -strategy.Volume : strategy.Volume;
        }

        public virtual bool LastBarIsUp(IEnumerable<CustomQuote> quotes)
        {
            var lastQuote = quotes.SkipLast(1).Last();
            return lastQuote.Close > lastQuote.Open;
        }
    }

    public class LastBarFt : LastBar
    {
        public LastBarFt(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override double SignalVolume(IEnumerable<CustomQuote> quotes)
            => base.SignalVolume(quotes) * -1;
    }

    public class DoubleRsi : IStrategy
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        public DoubleRsi(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            _operationSettings.Value.Strategy.DoubleRsi.SlowLookbackPeriods;

        public virtual double SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            return FastRsiIsGreaterThanSlowRsi(quotes) ? -strategy.Volume : strategy.Volume;
        }

        protected bool FastRsiIsGreaterThanSlowRsi(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var fastRsis = quotes.GetRsi(strategy.DoubleRsi.FastLookbackPeriods);
            var slowRsis = quotes.GetRsi(strategy.DoubleRsi.SlowLookbackPeriods);
            var fastRsi = fastRsis.Last();
            var slowRsi = slowRsis.Last();
            return fastRsi.Rsi > slowRsi.Rsi;
        }
    }

    public class DoubleRsiFt : DoubleRsi
    {
        public DoubleRsiFt(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override double SignalVolume(IEnumerable<CustomQuote> quotes)
            => base.SignalVolume(quotes) * -1;
    }

    public class Macd : IStrategy
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        public Macd(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            _operationSettings.Value.Strategy.Macd.SlowPeriods;

        public virtual double SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            return MacdIsGreaterThanSignal(quotes) ? -strategy.Volume : strategy.Volume;
        }

        protected bool MacdIsGreaterThanSignal(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Macd;
            var macds = quotes.GetMacd(settings.FastPeriods, settings.SlowPeriods, settings.SignalPeriods);
            var lastMacd = macds.Last();
            return lastMacd.Macd > lastMacd.Signal;
        }
    }

    public class MacdFt : Macd
    {
        public MacdFt(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override double SignalVolume(IEnumerable<CustomQuote> quotes)
            => base.SignalVolume(quotes) * -1;
    }

    public class SuperTrend : IStrategy
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        public SuperTrend(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            _operationSettings.Value.Strategy.SuperTrend.LookbackPeriods;

        public virtual double SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            return SuperTrendHasLowerBand(quotes) ? -strategy.Volume : strategy.Volume;
        }

        protected bool SuperTrendHasLowerBand(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.SuperTrend;
            var superTrends = quotes.GetSuperTrend(settings.LookbackPeriods, settings.Multiplier);
            var superTrend = superTrends.Last();
            return superTrend.LowerBand is not null;
        }
    }

    public class SuperTrendFt : SuperTrend
    {
        public SuperTrendFt(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override double SignalVolume(IEnumerable<CustomQuote> quotes)
            => base.SignalVolume(quotes) * -1;
    }

    public class Vwap : IStrategy
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        public Vwap(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            _operationSettings.Value.Strategy.Vwap.LookbackPeriods;

        public virtual double SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            return CloseIsGreaterThanVwap(quotes) ? -strategy.Volume : strategy.Volume;
        }

        protected bool CloseIsGreaterThanVwap(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Vwap;
            var fromQuote = quotes.SkipLast(settings.LookbackPeriods).Last();
            var vwaps = quotes.GetVwap(fromQuote.Date);
            var vwap = vwaps.Last();
            return Convert.ToDouble(quotes.Last().Close) > vwap.Vwap;
        }
    }

    public class VwapFt : Vwap
    {
        public VwapFt(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override double SignalVolume(IEnumerable<CustomQuote> quotes)
              => base.SignalVolume(quotes) * -1;
    }

    public class Kama : IStrategy
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        public Kama(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            _operationSettings.Value.Strategy.Kama.SlowPeriods;

        public virtual double SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            return CloseIsGreaterThanKama(quotes) ? -strategy.Volume : strategy.Volume;
        }

        protected bool CloseIsGreaterThanKama(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Kama;
            var kamas = quotes.GetKama(settings.ErPeriods, settings.FastPeriods, settings.SlowPeriods);
            var kama = kamas.Last();
            return Convert.ToDouble(quotes.Last().Close) > kama.Kama;
        }
    }

    public class KamaFt : Kama
    {
        public KamaFt(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override double SignalVolume(IEnumerable<CustomQuote> quotes)
            => base.SignalVolume(quotes) * -1;
    }

    public class HtTrendline : IStrategy
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        public HtTrendline(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods => 0;

        public virtual double SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            return SmoothPriceIsGreaterThanTrendline(quotes) ? -strategy.Volume : strategy.Volume;
        }

        protected bool SmoothPriceIsGreaterThanTrendline(IEnumerable<CustomQuote> quotes)
        {
            var htTrendlines = quotes.GetHtTrendline();
            var htTrendline = htTrendlines.Last();
            return htTrendline.SmoothPrice > htTrendline.Trendline;
        }
    }

    public class HtTrendlineFt : HtTrendline
    {
        public HtTrendlineFt(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override double SignalVolume(IEnumerable<CustomQuote> quotes)
            => base.SignalVolume(quotes) * -1;
    }

    public class Mama : IStrategy
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        public Mama(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods => 0;

        public virtual double SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            return MamaIsGreaterThanFama(quotes) ? -strategy.Volume : strategy.Volume;
        }

        protected bool MamaIsGreaterThanFama(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Mama;
            var mamas = quotes.GetMama(settings.FastLimit, settings.SlowLimit);
            var mama = mamas.Last();
            return mama.Mama > mama.Fama;
        }
    }

    public class MamaFt : Mama
    {
        public MamaFt(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override double SignalVolume(IEnumerable<CustomQuote> quotes)
            => base.SignalVolume(quotes) * -1;
    }

    public class T3 : IStrategy
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        public T3(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            _operationSettings.Value.Strategy.T3.LookbackPeriods;

        public virtual double SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            return CloseIsGreaterThanT3(quotes) ? -strategy.Volume : strategy.Volume;
        }

        protected bool CloseIsGreaterThanT3(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.T3;
            var t3s = quotes.GetT3(settings.LookbackPeriods, settings.VolumeFactor);
            var t3 = t3s.Last();
            return Convert.ToDouble(quotes.Last().Close) > t3.T3;
        }
    }

    public class T3Ft : T3
    {
        public T3Ft(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override double SignalVolume(IEnumerable<CustomQuote> quotes)
            => base.SignalVolume(quotes) * -1;
    }

    public class Alma : IStrategy
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        public Alma(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            _operationSettings.Value.Strategy.Alma.LookbackPeriods;

        public virtual double SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            return CloseIsGreaterThanAlma(quotes) ? -strategy.Volume : strategy.Volume;
        }

        protected bool CloseIsGreaterThanAlma(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Alma;
            var almas = quotes.GetAlma(settings.LookbackPeriods, settings.Offset, settings.Sigma);
            var alma = almas.Last();
            return Convert.ToDouble(quotes.Last().Close) > alma.Alma;
        }
    }

    public class AlmaFt : Alma
    {
        public AlmaFt(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override double SignalVolume(IEnumerable<CustomQuote> quotes)
            => base.SignalVolume(quotes) * -1;
    }
}
