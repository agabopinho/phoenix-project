using Application.Options;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;

namespace Application.Services.Strategies
{
    public class KeltnerAndEmaSignal : IStrategy
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        public KeltnerAndEmaSignal(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            Math.Max(
                _operationSettings.Value.Strategy.KeltnerAndEmaSignal.EmaLookbackPeriods,
                Math.Max(
                    _operationSettings.Value.Strategy.KeltnerAndEmaSignal.EmaPeriods,
                    _operationSettings.Value.Strategy.KeltnerAndEmaSignal.AtrPeriods));

        public virtual double SignalVolume(IEnumerable<IQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var keltnerSettings = strategy.KeltnerAndEmaSignal;

            var keltners = quotes.GetKeltner(keltnerSettings.EmaPeriods, keltnerSettings.Multipler, keltnerSettings.AtrPeriods);
            var emas = quotes.GetEma(keltnerSettings.EmaLookbackPeriods);

            var lastKeltner = keltners.Last();
            var lastEma = emas.Last().Ema;
            var lastClose = Convert.ToDouble(quotes.Last().Close);

            if (lastClose > lastEma && lastClose < lastKeltner.UpperBand)
                return -strategy.Volume;

            if (lastClose < lastEma && lastClose > lastKeltner.LowerBand)
                return strategy.Volume;

            return 0d;
        }
    }

    public class KeltnerAndEmaSignalFt : KeltnerAndEmaSignal
    {
        public KeltnerAndEmaSignalFt(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override double SignalVolume(IEnumerable<IQuote> quotes)
            => base.SignalVolume(quotes) * -1;
    }
}
