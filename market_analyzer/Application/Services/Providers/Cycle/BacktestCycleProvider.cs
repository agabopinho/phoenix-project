using Application.Options;
using Microsoft.Extensions.Options;

namespace Application.Services.Providers.Cycle
{
    public class BacktestCycleProvider : ICycleProvider
    {
        private readonly IOptions<OperationSettings> _operationSettings;

        private DateTime _current;

        public BacktestCycleProvider(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;

            Start = _operationSettings.Value.Strategy.Date.ToDateTime(operationSettings.Value.Strategy.Start, DateTimeKind.Utc);
            End = _operationSettings.Value.Strategy.Date.ToDateTime(operationSettings.Value.Strategy.End, DateTimeKind.Utc);
            Step = operationSettings.Value.Backtest.Step;
        }

        public DateTime Start { get; }
        public DateTime End { get; }
        public TimeSpan Step { get; }
        public TimeZoneInfo TimeZone => TimeZoneInfo.FindSystemTimeZoneById(_operationSettings.Value.Strategy.TimeZoneId!);
        public bool EndOfDay => _current >= End.Subtract(_operationSettings.Value.Strategy.Timeframe);

        public void TakeStep()
        {
            if (_current == DateTime.MinValue)
            {
                _current = Start;

                return;
            }

            _current += Step;
        }

        public DateTime Now() => _current;
    }
}
