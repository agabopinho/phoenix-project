using Application.Helpers;
using Application.Options;
using Microsoft.Extensions.Options;
using Nessos.LinqOptimizer.Core;
using Skender.Stock.Indicators;

namespace Application.Services.Strategies
{
    public class Chandelier : IStrategy.IWithPosition
    {
        private readonly IOptions<OperationSettings> _operationSettings;

        private decimal _lastOpen = 0;

        public Chandelier(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods => 1;

        public StrategyPosition? Position { get; set; }

        public double SignalVolume(IEnumerable<IQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;

            var quoteRanges = quotes
                .GetRange(15)
                .ToArray();

            if (quoteRanges.Length < 15)
                return 0;
            if (_lastOpen == quoteRanges[^1].Open)
                return 0;
            _lastOpen = quoteRanges[^1].Open;

            var fast = quoteRanges.GetRsi(5).Last().Rsi;
            var slow = quoteRanges.GetRsi(15).Last().Rsi;

            var l1IsUp = fast > slow;

            var upVolume = strategy.Volume;
            var downVolume = -strategy.Volume;

            if ((Position?.Volume ?? 0) == 0)
                if (l1IsUp)
                    return upVolume;
                else
                    return downVolume;

            if (Position!.Volume > 0)
                if (!l1IsUp)
                    return Position.Volume * -1;

            if (Position!.Volume < 0)
                if (l1IsUp)
                    return Position.Volume * -1;

            return 0;
        }
    }
}
