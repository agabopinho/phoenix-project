using Application.Helpers;
using Application.Options;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;

namespace Application.Services.Strategies
{
    public class KeltnerRainbow : IStrategy
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        public KeltnerRainbow(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            Math.Max(
                _operationSettings.Value.Strategy.KeltnerRainbow.SmaPeriods,
                _operationSettings.Value.Strategy.KeltnerRainbow.AtrPeriods);

        public virtual double SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            return GetVolumeMultipler(quotes) * strategy.Volume;
        }

        protected virtual int GetVolumeMultipler(IEnumerable<CustomQuote> quotes)
        {
            var settings = _operationSettings.Value.Strategy.KeltnerRainbow;
            var multipler = settings.Multipler;
            var resultBands = new List<KeltnerResult>(settings.Count);

            for (var i = 0; i < settings.Count; i++)
            {
                var bands = quotes.GetKeltner(settings.SmaPeriods, multipler, settings.AtrPeriods);

                resultBands.Add(bands.Last());
                multipler += settings.MultiplerStep;
            }

            var lastClose = Convert.ToDouble(quotes.Last().Close);
            var index = 0;

            for (var i = 1; i < resultBands.Count + 1; i++)
            {
                if (lastClose > resultBands[i - 1].UpperBand!)
                    index = -i;

                if (lastClose < resultBands[i - 1].LowerBand!)
                    index = i;
            }

            return index;
        }
    }

    public class KeltnerRainbowFt : KeltnerRainbow
    {
        public KeltnerRainbowFt(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override double SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.KeltnerRainbow;
            var multipler = GetVolumeMultipler(quotes) * -1;

            if (multipler != 0)
                if (multipler > 0)
                    multipler = settings.Count - multipler + 1;
                else
                    multipler = -settings.Count - multipler - 1;

            return multipler * strategy.Volume;
        }
    }
}
