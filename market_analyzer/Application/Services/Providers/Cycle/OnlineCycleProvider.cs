using Application.Options;
using Microsoft.Extensions.Options;

namespace Application.Services.Providers.Cycle
{
    public class OnlineCycleProvider : ICycleProvider
    {
        private readonly IOptions<OperationSettings> _operationSettings;

        private DateTime _previous;

        public OnlineCycleProvider(IOptions<OperationSettings> operationSettings)
        {
            _operationSettings = operationSettings;
        }

        public TimeZoneInfo TimeZone => TimeZoneInfo.FindSystemTimeZoneById(_operationSettings.Value.TimeZoneId!);
        public DateTime Previous => _previous;

        public DateTime PlatformNow()
        {
            _previous = DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZone), DateTimeKind.Utc);
            return _previous;
        }
    }
}
