using Application.Options;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;

namespace Application.Services.Strategies
{
    public class Renko : IStrategy
    {
        private readonly IStrategyFactory _strategyFactory;
        private readonly IOptions<OperationSettings> _operationSettings;

        private double _lastRenkoOpen = 0;

        public Renko(IStrategyFactory strategyFactory, IOptions<OperationSettings> operationSettings)
        {
            _strategyFactory = strategyFactory;
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
           _strategyFactory.Get(_operationSettings.Value.Strategy.Renko.Use)!.LookbackPeriods;

        public virtual double SignalVolume(IEnumerable<IQuote> quotes)
        {
            var settings = _operationSettings.Value.Strategy.Renko;

            var renkos = quotes
                .GetRenko(Convert.ToDecimal(settings.BrickSize), EndType.HighLow)
                .ToArray();

            if (!HasChanged(renkos))
                return 0d;

            return _strategyFactory
                .Get(settings.Use)!
                .SignalVolume(renkos);
        }

        private bool HasChanged(IEnumerable<IQuote> quotes)
        {
            if (!quotes.Any())
                return false;

            if (!_operationSettings.Value.Strategy.Renko.VerifyChanged)
                return true;

            var lastRenkoOpen = Convert.ToDouble(quotes.Last().Open);
            if (_lastRenkoOpen == lastRenkoOpen)
                return false;

            _lastRenkoOpen = lastRenkoOpen;

            return true;
        }
    }
}
