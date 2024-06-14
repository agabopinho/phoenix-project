using Application.Options;
using Microsoft.Extensions.Options;

namespace Application.Services.Providers.Date;

public class OnlineDate(IOptions<OperationSettings> operationSettings) : IDate
{
    private readonly IOptions<OperationSettings> _operationSettings = operationSettings;

    public TimeZoneInfo TimeZone => TimeZoneInfo.FindSystemTimeZoneById(_operationSettings.Value.TimeZoneId!);

    public DateTime LocalDateSpecifiedUtcKind()
    {
        return DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZone), DateTimeKind.Utc);
    }
}
