using Application.Helpers;
using Application.Options;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;

namespace Application.Services.Strategies
{
    public class Renko : IStrategy
    {
        private readonly IOptions<OperationSettings> _operationSettings;

        public Renko(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            _operationSettings.Value.Strategy.Renko.SlowLookbackPeriods;

        private double _lastRenkoOpen = 0;

        public virtual double SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Renko;

            var renkos = quotes
                .GetRenko(Convert.ToDecimal(settings.BrickSize), EndType.HighLow)
                .ToArray();

            if (!renkos.Any())
                return 0d;

            var lastRenkoOpen = Convert.ToDouble(renkos.Last().Open);
            if (_lastRenkoOpen == lastRenkoOpen)
                return 0d;
            _lastRenkoOpen = lastRenkoOpen;

            var lastFastEma = renkos.GetEma(settings.FastLookbackPeriods).Last().Ema;
            var lastSlowEma = renkos.GetEma(settings.SlowLookbackPeriods).Last().Ema;

            var volume = 0d;

            if (lastFastEma > lastSlowEma)
                volume = -strategy.Volume;

            if (lastFastEma < lastSlowEma)
                volume = strategy.Volume;

            return volume;
        }
    }

    public class RenkoFt : Renko
    {
        public RenkoFt(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override double SignalVolume(IEnumerable<CustomQuote> quotes)
            => base.SignalVolume(quotes) * -1;
    }
}
