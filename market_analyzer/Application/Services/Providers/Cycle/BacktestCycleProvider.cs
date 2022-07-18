using Application.Options;
using Microsoft.Extensions.Options;

namespace Application.Services.Providers.Cycle
{
    public class BacktestCycleProvider : ICycleProvider
    {
        private readonly IOptionsSnapshot<OperationSettings> _operationSettings;

        public BacktestCycleProvider(IOptionsSnapshot<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;

            var marketData = operationSettings.Value.MarketData;
            var backtest = operationSettings.Value.Backtest;

            Start = marketData.Date.ToDateTime(operationSettings.Value.Start);
            End = marketData.Date.ToDateTime(operationSettings.Value.End);
            Step = backtest.Step;

            NextDate = DateTime.SpecifyKind(Start, DateTimeKind.Utc);
        }

        public DateTime Start { get; }
        public DateTime End { get; }
        public TimeSpan Step { get; }
        public DateTime Previous { get; private set; }
        public DateTime NextDate { get; private set; }
        public TimeZoneInfo TimeZone => TimeZoneInfo.FindSystemTimeZoneById(_operationSettings.Value.MarketData.TimeZoneId!);

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
