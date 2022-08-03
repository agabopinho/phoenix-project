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

                var multipler = Math.Pow(2, Math.Min(settings.MaxPower, p / settings.Range));
                var enterEverySeconds = lastQuote.Date.TimeOfDay.TotalSeconds % settings.EnterEverySeconds;
                var totalSeconds = lastQuote.Date.TimeOfDay.TotalSeconds;

                if (multipler > _lastMultipler || enterEverySeconds > 0 && totalSeconds % enterEverySeconds == 0)
                {
                    _lastMultipler = multipler;

                    return -strategy.Volume * multipler;
                }
            }

            return 0d;
        }
    }
}
