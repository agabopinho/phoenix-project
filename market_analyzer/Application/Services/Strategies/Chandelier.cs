using Application.Options;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;

namespace Application.Services.Strategies
{
    public class Chandelier : IStrategy.IWithPosition
    {
        private readonly IOptions<OperationSettings> _operationSettings;

        public Chandelier(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods => 22;

        public StrategyPosition? Position { get; set; }

        public double SignalVolume(IEnumerable<IQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;

            var lastClose = Convert.ToDouble(quotes.Last().Close);

            var stopAtr = quotes.GetVolatilityStop(4, 1).ToArray();

            if (Position?.Volume > 0 && lastClose < stopAtr[^1].LowerBand)
                return Position.Volume * -1;

            if (Position?.Volume < 0 && lastClose > stopAtr[^1].UpperBand)
                return Position.Volume * -1;

            var indicators = quotes.GetMacd(6, 12).ToArray();

            var l0 = indicators[^2];
            var l1 = indicators[^3];

            if (l0.Macd > l0.Signal && l1.Macd < l1.Signal)
            {
                if (Position?.Volume < 0)
                    return 0;

                return (Position?.Volume ?? 0) * -1 + -strategy.Volume;
            }

            if (l0.Macd < l0.Signal && l1.Macd > l1.Signal)
            {
                if (Position?.Volume > 0)
                    return 0;

                return (Position?.Volume ?? 0) * -1 + strategy.Volume;
            }

            return 0;
        }
    }
}
