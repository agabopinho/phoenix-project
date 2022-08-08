using Application.Options;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;

namespace Application.Services.Strategies
{
    public class Trend : IStrategy.IWithPosition
    {
        private readonly IStrategyFactory _strategyFactory;
        private readonly IOptions<OperationSettings> _operationSettings;

        private double _multipler = 1;

        public Trend(IStrategyFactory strategyFactory, IOptions<OperationSettings> operationSettings)
        {
            _strategyFactory = strategyFactory;
            _operationSettings = operationSettings;
        }

        public StrategyPosition? Position { get; set; }

        public int LookbackPeriods =>
            _strategyFactory.Get(_operationSettings.Value.Strategy.Trend.Use)!.LookbackPeriods;

        public virtual double SignalVolume(IEnumerable<IQuote> quotes)
        {
            var settings = _operationSettings.Value.Strategy.Trend;
            var strategy = _strategyFactory.Get(settings.Use)!;
            var volume = strategy.SignalVolume(quotes);

            if (Position is null)
                return volume * Math.Pow(2, Math.Min(settings.MaxPower, GetMultipler()));

            if (Position.Volume > 0 && volume < 0)
            {
                GetMultipler();
                return Position.Volume * -1;
            }

            if (Position.Volume < 0 && volume > 0)
            {
                GetMultipler();
                return Position.Volume * -1;
            }

            if (volume == 0)
            {
                GetMultipler();
                return Position.Volume * -1;
            }

            return 0;
        }

        private double GetMultipler()
        {
            if (Position is null)
                return _multipler;

            if (Position.Profit < 0)
                _multipler++;
            else
                _multipler = 1;

            return _multipler;
        }
    }
}
