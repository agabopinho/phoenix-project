using Application.Options;
using Microsoft.Extensions.Options;

namespace Application.Services.Providers.Cycle
{
    public class BacktestCycleProvider : ICycleProvider
    {
        private readonly IOptions<OperationSettings> _operationSettings;

        public BacktestCycleProvider(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;

            var symbolData = operationSettings.Value.Symbol;
            var backtest = operationSettings.Value.Backtest;

            Start = _operationSettings.Value.Date.ToDateTime(operationSettings.Value.Start);
            End = _operationSettings.Value.Date.ToDateTime(operationSettings.Value.End);
            Step = backtest.Step;

            NextDate = DateTime.SpecifyKind(Start, DateTimeKind.Utc);
        }

        public DateTime Start { get; }
        public DateTime End { get; }
        public TimeSpan Step { get; }
        public DateTime Previous { get; private set; }
        public DateTime NextDate { get; private set; }
        public TimeZoneInfo TimeZone => TimeZoneInfo.FindSystemTimeZoneById(_operationSettings.Value.TimeZoneId!);

        public DateTime PlatformNow()
        {
            Previous = NextDate;
            NextDate = NextDate.Add(Step);

            if (Previous > End)
                throw new BacktestFinishException();

            return Previous;
        }
    }
}
