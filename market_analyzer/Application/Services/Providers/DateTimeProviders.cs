using Application.Options;
using Microsoft.Extensions.Options;

namespace Application.Services.Providers
{
    public interface IDateTimeProvider
    {
        TimeZoneInfo TimeZone { get; }

        DateTime Now();
    }

    public class OnlineDateTimeProvider : IDateTimeProvider
    {
        public OnlineDateTimeProvider(IOptionsSnapshot<OperationSettings> operationSettings)
        {
            var marketData = operationSettings.Value.MarketData;

            TimeZone = TimeZoneInfo.FindSystemTimeZoneById(marketData.TimeZoneId!);
        }

        public TimeZoneInfo TimeZone { get; }

        public DateTime Now()
            => DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZone), DateTimeKind.Utc);
    }

    public class BacktestDateTimeProvider : IDateTimeProvider
    {
        public BacktestDateTimeProvider(IOptionsSnapshot<OperationSettings> operationSettings)
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

        public DateTime Now()
        {
            PreviousDate = CurrentDate;
            CurrentDate = NextDate;
            NextDate = NextDate.Add(Step);

            return CurrentDate;
        }
    }
}
