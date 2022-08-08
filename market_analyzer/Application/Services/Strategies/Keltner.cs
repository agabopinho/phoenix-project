using Application.Helpers;
using Application.Options;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;

namespace Application.Services.Strategies
{
    public class Keltner : IStrategy
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        public Keltner(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            Math.Max(
                _operationSettings.Value.Strategy.Keltner.LookbackPeriods,
                Math.Max(
                    _operationSettings.Value.Strategy.Keltner.EmaPeriods,
                    _operationSettings.Value.Strategy.Keltner.AtrPeriods));

        public virtual double SignalVolume(IEnumerable<IQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var keltnerSettings = strategy.Keltner;

            var keltners = quotes.GetKeltner(keltnerSettings.EmaPeriods, keltnerSettings.Multipler, keltnerSettings.AtrPeriods);
            var emas = quotes.GetEma(keltnerSettings.LookbackPeriods);

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

    public class KeltnerFt : Keltner
    {
        public KeltnerFt(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override double SignalVolume(IEnumerable<IQuote> quotes)
            => base.SignalVolume(quotes) * -1;
    }
}
