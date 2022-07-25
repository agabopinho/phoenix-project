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

            Start = _operationSettings.Value.Date.ToDateTime(operationSettings.Value.Start, DateTimeKind.Utc);
            End = _operationSettings.Value.Date.ToDateTime(operationSettings.Value.End, DateTimeKind.Utc);
            Step = operationSettings.Value.Backtest.Step;

            NextDate = Start;
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
