using Application.Options;
using Microsoft.Extensions.Options;

namespace Application.Services.Providers.Cycle
{
    public class OnlineCycleProvider : ICycleProvider
    {
        public OnlineCycleProvider(IOptionsSnapshot<OperationSettings> operationSettings)
        {
            var marketData = operationSettings.Value.MarketData;

            TimeZone = TimeZoneInfo.FindSystemTimeZoneById(marketData.TimeZoneId!);
        }

        public TimeZoneInfo TimeZone { get; }

        public DateTime PlatformNow()
            => DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZone), DateTimeKind.Utc);
    }
}
