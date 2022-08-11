using Application.Helpers;
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
                .ToList<IQuote>();

            if (!HasChanged(quotes))
                return 0;

            AddLast(quotes, renkos);

            return _strategyFactory
                .Get(settings.Use)!
                .SignalVolume(renkos);
        }

        private bool HasChanged(IEnumerable<IQuote> quotes)
        {
            if (!quotes.Any())
                return false;

            if (!_operationSettings.Value.Strategy.RenkoAtr.FireOnlyAtCandleOpening)
                return true;

            var lastRenkoHigh = Convert.ToDouble(quotes.Last().High);
            var lastRenkoLow = Convert.ToDouble(quotes.Last().Low);
            if (_lastRenkoHigh == lastRenkoHigh ||
                _lastRenkoLow == lastRenkoLow)
                return false;

            _lastRenkoHigh = lastRenkoHigh;
            _lastRenkoLow = lastRenkoLow;

            return true;
        }

        private static void AddLast(IEnumerable<IQuote> quotes, List<IQuote> renkos)
        {
            var last = quotes.Last();

            renkos.Add(new CustomQuote
            {
                Date = last.Date,
                High = last.Close,
                Low = last.Close,
                Open = last.Close,
                Close = last.Close,
            });
        }
    }
}
