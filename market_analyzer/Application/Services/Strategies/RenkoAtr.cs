using Application.Options;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;

namespace Application.Services.Strategies
{
    public class RenkoAtr : IStrategy
    {
        private readonly IStrategyFactory _strategyFactory;
        private readonly IOptions<OperationSettings> _operationSettings;

        private double _lastRenkoHigh = 0;
        private double _lastRenkoLow = 0;

        public RenkoAtr(IStrategyFactory strategyFactory, IOptions<OperationSettings> operationSettings)
        {
            _strategyFactory = strategyFactory;
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods =>
            Math.Max(
                _operationSettings.Value.Strategy.RenkoAtr.AtrPeriods,
                _strategyFactory.Get(_operationSettings.Value.Strategy.RenkoAtr.Use)!.LookbackPeriods);

        public virtual double SignalVolume(IEnumerable<IQuote> quotes)
        {
            var settings = _operationSettings.Value.Strategy.RenkoAtr;

            var renkos = quotes
                .GetRenkoAtr(settings.AtrPeriods, EndType.HighLow)
                .ToArray();

            if (!renkos.Any())
                return 0d;

            var lastRenkoHigh = Convert.ToDouble(renkos.Last().High);
            var lastRenkoLow = Convert.ToDouble(renkos.Last().Low);
            if (_lastRenkoHigh == lastRenkoHigh || 
                _lastRenkoLow == lastRenkoLow)
                return 0d;
            _lastRenkoHigh = lastRenkoHigh;
            _lastRenkoLow = lastRenkoLow;

            return _strategyFactory
                .Get(settings.Use)!
                .SignalVolume(renkos);
        }
    }
}
