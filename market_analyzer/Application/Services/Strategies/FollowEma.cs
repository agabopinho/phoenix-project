using Application.Options;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;

namespace Application.Services.Strategies
{
    public class FollowEma : IStrategy.IWithPosition
    {
        protected readonly IOptions<OperationSettings> _operationSettings;

        private double _lastEntryPrice = 0;

        public FollowEma(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            _operationSettings.Value.Strategy.FollowEma.FastLookbackPeriods;

        public StrategyPosition? Position { get; set; }

        public virtual double SignalVolume(IEnumerable<IQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = _operationSettings.Value.Strategy.FollowEma;

            var fastMa = quotes.GetEma(settings.FastLookbackPeriods).Last().Ema;
            var lastClose = Convert.ToDouble(quotes.Last().Close);

            if (Math.Abs(_lastEntryPrice - lastClose) < settings.MinMoviment)
                return 0;

            if (lastClose > fastMa)
            {
                _lastEntryPrice = lastClose;

                if (Position?.Volume < 0)
                    return Position.Volume * -1;

                return strategy.Volume;
            }

            if (lastClose < fastMa)
            {
                _lastEntryPrice = lastClose;

                if (Position?.Volume > 0)
                    return Position.Volume * -1;

                return -strategy.Volume;
            }

            return 0;
        }
    }
}
