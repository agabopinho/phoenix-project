using Application.Helpers;
using Application.Options;
using Microsoft.Extensions.Options;
using Nessos.LinqOptimizer.Core;
using Skender.Stock.Indicators;

namespace Application.Services.Strategies
{
    public class Hope : IStrategy.IWithPosition
    {
        private readonly IOptions<OperationSettings> _operationSettings;

        public Hope(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods => 1;

        public StrategyPosition? Position { get; set; }

        public double SignalVolume(IEnumerable<IQuote> quotes)
        {
            var q = quotes.ToArray();

            var strategy = _operationSettings.Value.Strategy;

            var l1 = Convert.ToInt32(strategy.Hope["L1"]);
            var l2 = Convert.ToInt32(strategy.Hope["L2"]);

            if (!(q.Length >= Math.Max(l1, l2)))
                return 0;

            var volume = (Position?.Volume ?? 0);
            var profit = (Position?.Profit ?? 0);

            var l1Value = q[^l1].Close;

            var high = q[^l2..^l1].Max(it => it.High);
            var low = q[^l2..^l1].Min(it => it.Low);

            var distanceFromHigh = Math.Abs(l1Value - high);
            var distanceFromLow = Math.Abs(l1Value - low);

            if (distanceFromLow > distanceFromHigh)
                return Buy(volume);

            if (distanceFromHigh > distanceFromLow)
                return Sell(volume);

            return 0;
        }

        private static double Buy(double volume)
        {
            if (volume < 0)
                return volume * -1;

            if (volume > 0)
                return 0;

            return 1;
        }

        private static double Sell(double volume)
        {
            if (volume > 0)
                return volume * -1;

            if (volume < 0)
                return 0;

            return -1;
        }
    }
}
