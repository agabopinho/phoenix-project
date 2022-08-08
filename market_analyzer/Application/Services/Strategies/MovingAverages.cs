using Application.Helpers;
using Application.Options;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;

namespace Application.Services.Strategies
{
    public abstract class MaBase : IStrategy
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        public MaBase(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            _operationSettings.Value.Strategy.Ma.LookbackPeriods;

        public virtual double SignalVolume(IEnumerable<IQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            return CloseIsGreaterThanMa(quotes) ? -strategy.Volume : strategy.Volume;
        }

        protected abstract bool CloseIsGreaterThanMa(IEnumerable<IQuote> quotes);
    }

    public class Sma : MaBase
    {
        public Sma(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        protected override bool CloseIsGreaterThanMa(IEnumerable<IQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Ma;
            var emas = quotes.GetSma(settings.LookbackPeriods);
            var ema = emas.Last();
            return Convert.ToDouble(quotes.Last().Close) > ema.Sma;
        }
    }

    public class SmaFt : Sma
    {
        public SmaFt(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override double SignalVolume(IEnumerable<IQuote> quotes)
            => base.SignalVolume(quotes) * -1;
    }

    public class Ema : MaBase
    {
        public Ema(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        protected override bool CloseIsGreaterThanMa(IEnumerable<IQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Ma;
            var emas = quotes.GetEma(settings.LookbackPeriods);
            var ema = emas.Last();
            return Convert.ToDouble(quotes.Last().Close) > ema.Ema;
        }
    }

    public class EmaFt : Ema
    {
        public EmaFt(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override double SignalVolume(IEnumerable<IQuote> quotes)
            => base.SignalVolume(quotes) * -1;
    }

    public class Wma : MaBase
    {
        public Wma(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        protected override bool CloseIsGreaterThanMa(IEnumerable<IQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Ma;
            var emas = quotes.GetWma(settings.LookbackPeriods);
            var ema = emas.Last();
            return Convert.ToDouble(quotes.Last().Close) > ema.Wma;
        }
    }

    public class WmaFt : Wma
    {
        public WmaFt(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override double SignalVolume(IEnumerable<IQuote> quotes)
            => base.SignalVolume(quotes) * -1;
    }

    public class Vwma : MaBase
    {
        public Vwma(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        protected override bool CloseIsGreaterThanMa(IEnumerable<IQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Ma;
            var emas = quotes.GetVwma(settings.LookbackPeriods);
            var ema = emas.Last();
            return Convert.ToDouble(quotes.Last().Close) > ema.Vwma;
        }
    }

    public class VwmaFt : Vwma
    {
        public VwmaFt(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override double SignalVolume(IEnumerable<IQuote> quotes)
            => base.SignalVolume(quotes) * -1;
    }

    public class Dema : MaBase
    {
        public Dema(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        protected override bool CloseIsGreaterThanMa(IEnumerable<IQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Ma;
            var emas = quotes.GetDema(settings.LookbackPeriods);
            var ema = emas.Last();
            return Convert.ToDouble(quotes.Last().Close) > ema.Dema;
        }
    }

    public class DemaFt : Dema
    {
        public DemaFt(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override double SignalVolume(IEnumerable<IQuote> quotes)
            => base.SignalVolume(quotes) * -1;
    }

    public class Epma : MaBase
    {
        public Epma(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        protected override bool CloseIsGreaterThanMa(IEnumerable<IQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Ma;
            var emas = quotes.GetEpma(settings.LookbackPeriods);
            var ema = emas.Last();
            return Convert.ToDouble(quotes.Last().Close) > ema.Epma;
        }
    }

    public class EpmaFt : Epma
    {
        public EpmaFt(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override double SignalVolume(IEnumerable<IQuote> quotes)
            => base.SignalVolume(quotes) * -1;
    }

    public class Hma : MaBase
    {
        public Hma(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        protected override bool CloseIsGreaterThanMa(IEnumerable<IQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Ma;
            var emas = quotes.GetHma(settings.LookbackPeriods);
            var ema = emas.Last();
            return Convert.ToDouble(quotes.Last().Close) > ema.Hma;
        }
    }

    public class HmaFt : Hma
    {
        public HmaFt(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override double SignalVolume(IEnumerable<IQuote> quotes)
            => base.SignalVolume(quotes) * -1;
    }

    public class Smma : MaBase
    {
        public Smma(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        protected override bool CloseIsGreaterThanMa(IEnumerable<IQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Ma;
            var emas = quotes.GetSmma(settings.LookbackPeriods);
            var ema = emas.Last();
            return Convert.ToDouble(quotes.Last().Close) > ema.Smma;
        }
    }

    public class SmmaFt : Smma
    {
        public SmmaFt(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override double SignalVolume(IEnumerable<IQuote> quotes)
            => base.SignalVolume(quotes) * -1;
    }

    public class Tema : MaBase
    {
        public Tema(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        protected override bool CloseIsGreaterThanMa(IEnumerable<IQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Ma;
            var emas = quotes.GetTema(settings.LookbackPeriods);
            var ema = emas.Last();
            return Convert.ToDouble(quotes.Last().Close) > ema.Tema;
        }
    }

    public class TemaFt : Tema
    {
        public TemaFt(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override double SignalVolume(IEnumerable<IQuote> quotes)
            => base.SignalVolume(quotes) * -1;
    }
}
