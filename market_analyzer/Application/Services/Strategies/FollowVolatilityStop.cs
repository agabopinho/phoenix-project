using Application.Options;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;

namespace Application.Services.Strategies
{
    public class FollowVolatilityStop : IStrategy.IWithPosition
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        public FollowVolatilityStop(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            _operationSettings.Value.Strategy.FollowVolatilityStop.LookbackPeriods;

        public StrategyPosition? Position { get; set; }

        public virtual double SignalVolume(IEnumerable<IQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = _operationSettings.Value.Strategy.FollowVolatilityStop;
            var stopAtrs = quotes.GetVolatilityStop(settings.LookbackPeriods, settings.Multiplier);
            var stopAtr = stopAtrs.Last();

            if (stopAtr.LowerBand is not null)
            {
                if (Position?.Volume < 0)
                    return Position.Volume * -1;

                return strategy.Volume;
            }

            if (stopAtr.UpperBand is not null)
            {
                if (Position?.Volume > 0)
                    return Position.Volume * -1;

                return -strategy.Volume;
            }

            return 0;
        }
    }
}
