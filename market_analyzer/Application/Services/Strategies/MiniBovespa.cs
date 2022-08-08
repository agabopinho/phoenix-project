using Application.Options;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;

namespace Application.Services.Strategies
{
    public class MiniBovespa : IStrategy
    {
        private readonly IStrategyFactory _strategyFactory;
        private readonly IOptions<OperationSettings> _operationSettings;

        public MiniBovespa(IStrategyFactory strategyFactory, IOptions<OperationSettings> operationSettings)
        {
            _strategyFactory = strategyFactory;
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods => 0;

        public double SignalVolume(IEnumerable<IQuote> quotes)
        {
            var operationSettings = _operationSettings.Value;
            var miniBovespa = operationSettings.Strategy.MiniBovespa;

            var open = Convert.ToDouble(quotes.First().Open);
            var high = Convert.ToDouble(quotes.Max(it => it.High));
            var low = Convert.ToDouble(quotes.Min(it => it.Low));

            var highP = (high - open) / open * 100;
            var lowP = (open - low) / open * 100;

            if (highP >= miniBovespa.StartHighP && lowP >= miniBovespa.MinLowP || lowP >= miniBovespa.StartLowP && highP >= miniBovespa.MinHighP)
            {
                var strategy = _strategyFactory.Get(miniBovespa.Use)!;
                var volume = strategy.SignalVolume(quotes);

                return volume;
            }

            return 0d;
        }
    }
}
