using Application.Helpers;
using Application.Options;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;

namespace Application.Services.Strategies
{
    public abstract class Ma : IStrategy
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        public Ma(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            _operationSettings.Value.Strategy.Ma.LookbackPeriods;

        public virtual decimal SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            return CloseIsGreaterThanMa(quotes) ? -strategy.Volume : strategy.Volume;
        }

        protected abstract bool CloseIsGreaterThanMa(IEnumerable<CustomQuote> quotes);
    }

    public abstract class MaFollowTrend : Ma
    {
        protected MaFollowTrend(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override decimal SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            return !CloseIsGreaterThanMa(quotes) ? -strategy.Volume : strategy.Volume;
        }
    }

    public class Sma : Ma
    {
        public Sma(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        protected override bool CloseIsGreaterThanMa(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Ma;
            var emas = quotes.GetSma(settings.LookbackPeriods);
            var ema = emas.Last();
            return Convert.ToDouble(quotes.Last().Close) > ema.Sma;
        }
    }

    public class SmaFollowTrend : MaFollowTrend
    {
        public SmaFollowTrend(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        protected override bool CloseIsGreaterThanMa(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Ma;
            var emas = quotes.GetSma(settings.LookbackPeriods);
            var ema = emas.Last();
            return Convert.ToDouble(quotes.Last().Close) > ema.Sma;
        }
    }

    public class Ema : Ma
    {
        public Ema(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        protected override bool CloseIsGreaterThanMa(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Ma;
            var emas = quotes.GetEma(settings.LookbackPeriods);
            var ema = emas.Last();
            return Convert.ToDouble(quotes.Last().Close) > ema.Ema;
        }
    }

    public class EmaFollowTrend : MaFollowTrend
    {
        public EmaFollowTrend(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        protected override bool CloseIsGreaterThanMa(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Ma;
            var emas = quotes.GetEma(settings.LookbackPeriods);
            var ema = emas.Last();
            return Convert.ToDouble(quotes.Last().Close) > ema.Ema;
        }
    }

    public class Wma : Ma
    {
        public Wma(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        protected override bool CloseIsGreaterThanMa(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Ma;
            var emas = quotes.GetWma(settings.LookbackPeriods);
            var ema = emas.Last();
            return Convert.ToDouble(quotes.Last().Close) > ema.Wma;
        }
    }

    public class WmaFollowTrend : MaFollowTrend
    {
        public WmaFollowTrend(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        protected override bool CloseIsGreaterThanMa(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Ma;
            var emas = quotes.GetWma(settings.LookbackPeriods);
            var ema = emas.Last();
            return Convert.ToDouble(quotes.Last().Close) > ema.Wma;
        }
    }

    public class Vwma : Ma
    {
        public Vwma(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        protected override bool CloseIsGreaterThanMa(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Ma;
            var emas = quotes.GetVwma(settings.LookbackPeriods);
            var ema = emas.Last();
            return Convert.ToDouble(quotes.Last().Close) > ema.Vwma;
        }
    }

    public class VwmaFollowTrend : MaFollowTrend
    {
        public VwmaFollowTrend(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        protected override bool CloseIsGreaterThanMa(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Ma;
            var emas = quotes.GetVwma(settings.LookbackPeriods);
            var ema = emas.Last();
            return Convert.ToDouble(quotes.Last().Close) > ema.Vwma;
        }
    }

    public class Dema : Ma
    {
        public Dema(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        protected override bool CloseIsGreaterThanMa(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Ma;
            var emas = quotes.GetDema(settings.LookbackPeriods);
            var ema = emas.Last();
            return Convert.ToDouble(quotes.Last().Close) > ema.Dema;
        }
    }

    public class DemaFollowTrend : MaFollowTrend
    {
        public DemaFollowTrend(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        protected override bool CloseIsGreaterThanMa(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Ma;
            var emas = quotes.GetDema(settings.LookbackPeriods);
            var ema = emas.Last();
            return Convert.ToDouble(quotes.Last().Close) > ema.Dema;
        }
    }

    public class Epma : Ma
    {
        public Epma(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        protected override bool CloseIsGreaterThanMa(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Ma;
            var emas = quotes.GetEpma(settings.LookbackPeriods);
            var ema = emas.Last();
            return Convert.ToDouble(quotes.Last().Close) > ema.Epma;
        }
    }

    public class EpmaFollowTrend : MaFollowTrend
    {
        public EpmaFollowTrend(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        protected override bool CloseIsGreaterThanMa(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Ma;
            var emas = quotes.GetEpma(settings.LookbackPeriods);
            var ema = emas.Last();
            return Convert.ToDouble(quotes.Last().Close) > ema.Epma;
        }
    }

    public class Hma : Ma
    {
        public Hma(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        protected override bool CloseIsGreaterThanMa(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Ma;
            var emas = quotes.GetHma(settings.LookbackPeriods);
            var ema = emas.Last();
            return Convert.ToDouble(quotes.Last().Close) > ema.Hma;
        }
    }

    public class HmaFollowTrend : MaFollowTrend
    {
        public HmaFollowTrend(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        protected override bool CloseIsGreaterThanMa(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Ma;
            var emas = quotes.GetHma(settings.LookbackPeriods);
            var ema = emas.Last();
            return Convert.ToDouble(quotes.Last().Close) > ema.Hma;
        }
    }

    public class Smma : Ma
    {
        public Smma(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        protected override bool CloseIsGreaterThanMa(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Ma;
            var emas = quotes.GetSmma(settings.LookbackPeriods);
            var ema = emas.Last();
            return Convert.ToDouble(quotes.Last().Close) > ema.Smma;
        }
    }

    public class SmmaFollowTrend : MaFollowTrend
    {
        public SmmaFollowTrend(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        protected override bool CloseIsGreaterThanMa(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Ma;
            var emas = quotes.GetSmma(settings.LookbackPeriods);
            var ema = emas.Last();
            return Convert.ToDouble(quotes.Last().Close) > ema.Smma;
        }
    }

    public class Tema : Ma
    {
        public Tema(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        protected override bool CloseIsGreaterThanMa(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Ma;
            var emas = quotes.GetTema(settings.LookbackPeriods);
            var ema = emas.Last();
            return Convert.ToDouble(quotes.Last().Close) > ema.Tema;
        }
    }

    public class TemaFollowTrend : MaFollowTrend
    {
        public TemaFollowTrend(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        protected override bool CloseIsGreaterThanMa(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Ma;
            var emas = quotes.GetTema(settings.LookbackPeriods);
            var ema = emas.Last();
            return Convert.ToDouble(quotes.Last().Close) > ema.Tema;
        }
    }
}
