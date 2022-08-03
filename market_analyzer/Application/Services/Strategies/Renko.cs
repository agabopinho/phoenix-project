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

            var renkos = quotes
                .GetRenko(Convert.ToDecimal(settings.BrickSize), EndType.HighLow)
                .ToArray();

            if (!renkos.Any())
                return 0;

            var lastRenko = renkos[^1];

            var volume = 0d;

            if (lastRenko.IsUp)
                volume = -strategy.Volume;

            if (!lastRenko.IsUp)
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
