using Application.Helpers;
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

        public int LookbackPeriods => 1;

        public StrategyPosition? Position { get; set; }

        public double SignalVolume(IEnumerable<IQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;

            var quoteRanges = quotes
                .GetRange(20)
                .ToArray();

            var s = 12;
            var m = 1d;

            if (quoteRanges.Length < s)
                return 0;

            var ema = quoteRanges
                .GetVolatilityStop(s, m)
                .SkipLast(1)
                .Last();

            if (ema.UpperBand is not null)
            {
                if (Position?.Volume < 0)
                    return Position.Volume * -1;

                if (Position?.Volume > 0)
                    return 0;

                return strategy.Volume;
            }

            if (ema.LowerBand is not null)
            {
                if (Position?.Volume > 0)
                    return Position.Volume * -1;

                if (Position?.Volume < 0)
                    return 0;

                return -strategy.Volume;
            }

            return 0;
        }
    }
}
