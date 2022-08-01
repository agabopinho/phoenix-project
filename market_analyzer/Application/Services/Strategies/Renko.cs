using Application.Helpers;
using Application.Options;
using Microsoft.Extensions.Options;
using Skender.Stock.Indicators;

namespace Application.Services.Strategies
{
    public class Renko : IStrategy
    {
        private readonly IOptions<OperationSettings> _operationSettings;

        public Renko(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public int LookbackPeriods => 0;

        public virtual double SignalVolume(IEnumerable<CustomQuote> quotes)
        {
            var strategy = _operationSettings.Value.Strategy;
            var settings = strategy.Renko;

            var renkos = new[] { quotes.First() }
                .Concat(quotes
                    .TakeLast(100)
                    .ToArray())
                .GetRenko(Convert.ToDecimal(settings.BrickSize), EndType.HighLow);

            var lastRenko = renkos.SkipLast(1).Last();
            var previousRenko = renkos.SkipLast(2).Last();
            var previousRenko2 = renkos.SkipLast(3).Last();

            var volume = 0d;

            if (lastRenko.Open > previousRenko.Open && previousRenko.Open < previousRenko2.Open)
                volume = -strategy.Volume;

            if (lastRenko.Open < previousRenko.Open && previousRenko.Open > previousRenko2.Open)
                volume = strategy.Volume;

            return volume;
        }
    }

    public class RenkoFt : Renko
    {
        public RenkoFt(IOptions<OperationSettings> operationSettings) : base(operationSettings)
        {
        }

        public override double SignalVolume(IEnumerable<CustomQuote> quotes)
            => base.SignalVolume(quotes) * -1;
    }
}
