using Application.Helpers;
using Application.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;

namespace Application.Services.Strategies
{
    public class MiniBovespa : IStrategy
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IOptions<OperationSettings> _operationSettings;

        public MiniBovespa(IServiceProvider serviceProvider, IOptions<OperationSettings> operationSettings)
        {
            _serviceProvider = serviceProvider;
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
                var strategy = StrategyFactory.Get(miniBovespa.Use)!;
                var volume = strategy.SignalVolume(quotes);

                return volume;
            }

            return 0d;
        }

        private IStrategyFactory StrategyFactory
            => _serviceProvider.GetRequiredService<IStrategyFactory>();
    }
}
