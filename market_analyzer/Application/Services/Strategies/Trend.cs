using Application.Helpers;
using Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Application.Services.Strategies
{
    public class Trend : IStrategy.IWithPosition
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptions<OperationSettings> _operationSettings;

        private double _multipler = 1;

        public Trend(IServiceProvider serviceProvider, IOptions<OperationSettings> operationSettings)
        {
            _serviceProvider = serviceProvider;
            _operationSettings = operationSettings;
        }

        public StrategyPosition? Position { get; set; }

        public int LookbackPeriods =>
            StrategyFactory.Get(_operationSettings.Value.Strategy.Trend.Name)!.LookbackPeriods;

        private IStrategyFactory StrategyFactory
            => _serviceProvider.GetRequiredService<IStrategyFactory>();

        public virtual double SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var settings = _operationSettings.Value.Strategy.Trend;
            var strategy = StrategyFactory.Get(settings.Name)!;
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
