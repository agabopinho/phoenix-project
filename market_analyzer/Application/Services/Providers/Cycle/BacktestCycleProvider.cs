using Application.Options;
using Microsoft.Extensions.Options;

namespace Application.Services.Providers.Cycle
{
    public class BacktestCycleProvider : ICycleProvider
    {
        public BacktestCycleProvider(IOptionsSnapshot<OperationSettings> operationSettings)
        {
            TimeZone = TimeZoneInfo.Utc;

            var marketData = operationSettings.Value.MarketData;
            var backtest = operationSettings.Value.Backtest;

            NextDate = DateTime.SpecifyKind(marketData.Date.ToDateTime(backtest.Start), DateTimeKind.Utc);
            Step = backtest.Step;
        }

        public DateTime PreviousDate { get; private set; }
        public DateTime CurrentDate { get; private set; }
        public DateTime NextDate { get; private set; }
        public TimeSpan Step { get; }
        public TimeZoneInfo TimeZone { get; }

        public DateTime PlatformNow()
        {
            PreviousDate = CurrentDate;
            CurrentDate = NextDate;
            NextDate = NextDate.Add(Step);

            return CurrentDate;
        }
    }
}
