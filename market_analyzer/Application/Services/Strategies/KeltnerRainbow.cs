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
                _operationSettings.Value.Strategy.KeltnerRainbow.EmaPeriods,
                _operationSettings.Value.Strategy.KeltnerRainbow.AtrPeriods);

        public virtual double SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            return GetVolumeMultipler(quotes) * strategy.Volume;
        }

        protected virtual int GetVolumeMultipler(IEnumerable<CustomQuote> quotes)
        {
            var settings = _operationSettings.Value.Strategy.KeltnerRainbow;
            
            var resultBands = new List<KeltnerResult>(settings.Count);
            var multipler = settings.Multipler;

            for (var i = 0; i < settings.Count; i++)
            {
                resultBands.Add(quotes
                    .GetKeltner(settings.EmaPeriods, multipler, settings.AtrPeriods)
                    .Last());

                multipler += settings.MultiplerStep;
            }

            var lastClose = Convert.ToDouble(quotes.Last().Close);
            var index = 0;

            foreach (var band in resultBands)
            {
                if (lastClose > band.UpperBand!)
                    index--;

                if (lastClose < band.LowerBand!)
                    index++;
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
            return base.SignalVolume(quotes) * -1;
            var multipler = base.SignalVolume(quotes) * -1 / _operationSettings.Value.Strategy.Volume;
            var count = _operationSettings.Value.Strategy.KeltnerRainbow.Count;

            if (multipler > 0)
                return (count + 1 - multipler) * _operationSettings.Value.Strategy.Volume;

            if (multipler < 0)
                return (-count + -1 - multipler) * _operationSettings.Value.Strategy.Volume;

            return 0;
        }
    }
}
