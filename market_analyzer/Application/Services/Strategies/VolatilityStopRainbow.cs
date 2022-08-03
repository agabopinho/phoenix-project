using Application.Helpers;
using Application.Options;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;

namespace Application.Services.Strategies
{
    public class VolatilityStopRainbow : IStrategy
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        public VolatilityStopRainbow(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            _operationSettings.Value.Strategy.VolatilityStopRainbow.LookbackPeriods;

        public virtual double SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            return GetVolumeMultipler(quotes) * strategy.Volume;
        }

        protected virtual int GetVolumeMultipler(IEnumerable<CustomQuote> quotes)
        {
            var settings = _operationSettings.Value.Strategy.VolatilityStopRainbow;

            var resultBands = new List<VolatilityStopResult>(settings.Count);
            var multipler = settings.Multipler;

            for (var i = 0; i < settings.Count; i++)
            {
                resultBands.Add(quotes
                    .GetVolatilityStop(settings.LookbackPeriods, multipler)
                    .Last());

                multipler += settings.MultiplerStep;
            }

            var index = 0;

            for (var i = 1; i < resultBands.Count + 1; i++)
            {
                var band = resultBands[i - 1];

                if (band.UpperBand.HasValue)
                    index--;

                if (band.LowerBand.HasValue)
                    index++;
            }

            return index;
        }
    }

    public class VolatilityStopRainbowFt : VolatilityStopRainbow
    {
        public VolatilityStopRainbowFt(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override double SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var volume = base.SignalVolume(quotes) * -1;
            var count = _operationSettings.Value.Strategy.VolatilityStopRainbow.Count;

            if (volume > 0)
                return count + 1 - volume;

            if (volume < 0)
                return -count + -1 - volume;

            return 0;
        }
    }
}
