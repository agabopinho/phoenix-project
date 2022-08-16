using Application.Options;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;

namespace Application.Services.Strategies
{
    public class Stoch : IStrategy.IWithPosition
    {
        private readonly IOptions<OperationSettings> _operationSettings;

        public Stoch(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            _operationSettings.Value.Strategy.Stoch.LookbackPeriods;

        public StrategyPosition? Position { get; set; }

        public double SignalVolume(IEnumerable<IQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = _operationSettings.Value.Strategy.Stoch;

            var stochs = quotes.GetStoch(
                settings.LookbackPeriods,
                settings.SignalPeriods,
                settings.SmoothPeriods,
                settings.KFactor,
                settings.DFactor,
                settings.MaType).ToArray();

            var last = stochs.Last();

            if (last.Oscillator <= settings.Oversold && last.Oscillator > last.Signal)
            {
                if (Position?.Volume < 0)
                    return 0;

                return (Position?.Volume ?? 0) * -1 + -strategy.Volume;
            }

            if (last.Oscillator >= settings.Overbought && last.Oscillator < last.Signal)
            {
                if (Position?.Volume > 0)
                    return 0;

                return (Position?.Volume ?? 0) * -1 + strategy.Volume;
            }

            return 0;
        }
    }
}
