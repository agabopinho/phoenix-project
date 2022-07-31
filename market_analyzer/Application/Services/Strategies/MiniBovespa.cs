using Application.Helpers;
using Application.Options;
using Microsoft.Extensions.Options;

namespace Application.Services.Strategies
{
    public class MiniBovespa : IStrategy
    {
        private readonly IOptions<OperationSettings> _operationSettings;

        private double _lastMultipler;

        public MiniBovespa(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
            _lastMultipler = operationSettings.Value.Strategy.MiniBovespa.LastMultipler;
        }

        public int LookbackPeriods => 0;

        public double SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var operationSettings = _operationSettings.Value;
            var strategy = operationSettings.Strategy;
            var settings = strategy.MiniBovespa;
            var lastQuote = quotes.Last();

            var firstQuoteDate = operationSettings.Date.ToDateTime(operationSettings.Start);

            if (lastQuote.Date < firstQuoteDate)
                return 0d;

            if (firstQuoteDate == lastQuote.Date)
                return -strategy.Volume;

            var first = quotes.First(it => it.Date == firstQuoteDate);
            var p = Convert.ToDouble(lastQuote.Close) - Convert.ToDouble(first.Open);

            if (p / settings.Range >= 1)
            {
                p -= p % settings.Range;

                var multipler = Math.Pow(2, p / settings.Range);

                if (multipler > _lastMultipler)
                {
                    _lastMultipler = multipler;

                    if (multipler > settings.MaxMultipler)
                        return -strategy.Volume * settings.MaxMultipler;

                    return -strategy.Volume * multipler;
                }
            }

            return 0d;
        }
    }
}
