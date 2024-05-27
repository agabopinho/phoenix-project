using Application.Options;
using Microsoft.Extensions.Options;

namespace Application.Services.Providers.Date;

public class OnlineDateProvider(IOptions<OperationSettings> operationSettings) : IDateProvider
{
    private readonly IOptions<OperationSettings> _operationSettings = operationSettings;

    public TimeZoneInfo TimeZone => TimeZoneInfo.FindSystemTimeZoneById(_operationSettings.Value.TimeZoneId!);

    public DateTime LocalDateSpecifiedUtcKind()
    {
        return DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZone), DateTimeKind.Utc);
    }
}
