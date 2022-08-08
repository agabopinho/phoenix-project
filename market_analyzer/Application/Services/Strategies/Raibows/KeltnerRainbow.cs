using Application.Options;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;

namespace Application.Services.Strategies.Raibows
{
    public class KeltnerRainbow : IStrategy
    {
        private readonly IStrategyFactory _strategyFactory;
        protected readonly IOptions<OperationSettings> _operationSettings;

        public KeltnerRainbow(IStrategyFactory strategyFactory, IOptions<OperationSettings> operationSettings)
        {
            _strategyFactory = strategyFactory;
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            Math.Max(
                _operationSettings.Value.Strategy.KeltnerRainbow.EmaPeriods,
                _operationSettings.Value.Strategy.KeltnerRainbow.AtrPeriods);

        public virtual double SignalVolume(IEnumerable<IQuote> quotes)
        {
            var settings = _operationSettings.Value.Strategy.KeltnerRainbow;
            var volume = _strategyFactory.Get(settings.Use)!.SignalVolume(quotes);
            return GetVolumeMultipler(quotes, volume) * volume;
        }

        protected virtual int GetVolumeMultipler(IEnumerable<IQuote> quotes, double volume, bool followTrend = false)
        {
            if (volume == 0)
                return 0;

            var settings = _operationSettings.Value.Strategy.KeltnerRainbow;

            var resultBands = new List<double>(settings.Count * 2);
            var multipler = settings.Multipler;

            for (var i = 0; i < settings.Count; i++)
            {
                var band = quotes
                    .GetKeltner(settings.EmaPeriods, multipler, settings.AtrPeriods)
                    .Last();

                if (band.UpperBand is not null)
                    resultBands.Add(band.UpperBand.Value);

                if (band.LowerBand is not null)
                    resultBands.Add(band.LowerBand.Value);

                multipler += settings.MultiplerStep;
            }

            resultBands.Sort();

            var lastClose = Convert.ToDouble(quotes.Last().Close);
            var m = 0;

            foreach (var band in resultBands)
            {
                if (!followTrend)
                {
                    if (volume > 0 && lastClose < band)
                        m++;

                    if (volume < 0 && lastClose > band)
                        m++;

                    continue;
                }

                if (volume > 0 && lastClose > band)
                    m++;

                if (volume < 0 && lastClose < band)
                    m++;
            }

            return m;
        }
    }

    public class KeltnerRainbowFt : KeltnerRainbow
    {
        private readonly IStrategyFactory _strategyFactory;

        public KeltnerRainbowFt(IStrategyFactory strategyFactory, IOptions<OperationSettings> operationSettings) : base(strategyFactory, operationSettings)
        {
            _strategyFactory = strategyFactory;
        }

        public override double SignalVolume(IEnumerable<IQuote> quotes)
        {
            var settings = _operationSettings.Value.Strategy.KeltnerRainbow;
            var volume = _strategyFactory.Get(settings.Use)!.SignalVolume(quotes);
            return GetVolumeMultipler(quotes, volume, followTrend: true) * volume;
        }
    }
}
