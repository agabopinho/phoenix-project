using Application.Options;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;

namespace Application.Services.Strategies
{
    public class DoubleEma : IStrategy.IWithPosition
    {
        private readonly IOptions<OperationSettings> _operationSettings;

        public DoubleEma(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            _operationSettings.Value.Strategy.DoubleEma.SlowLookbackPeriods;

        public StrategyPosition? Position { get; set; }

        public double SignalVolume(IEnumerable<IQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = _operationSettings.Value.Strategy.DoubleEma;

            var fastMa = quotes.GetEma(settings.FastLookbackPeriods).ToArray();
            var slowMa = quotes.GetEma(settings.SlowLookbackPeriods).ToArray();

            var f0 = fastMa[^1].Ema;
            var f1 = fastMa[^2].Ema;

            var s0 = slowMa[^1].Ema;
            var s1 = slowMa[^2].Ema;

            if (f1 < s1 && f0 > s0)
            {
                if (Position?.Volume < 0)
                    return 0;

                return (Position?.Volume ?? 0) * -1 + -strategy.Volume;
            }

            if (f1 > s1 && f0 < s0)
            {
                if (Position?.Volume > 0)
                    return 0;

                return (Position?.Volume ?? 0) * -1 + strategy.Volume;
            }

            return 0;
        }
    }
}
