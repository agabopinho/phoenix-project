namespace Application.Services.Providers.Cycle
{
    public interface ICycleProvider
    {
        TimeZoneInfo TimeZone { get; }

        DateTime PlatformNow();
    }
}
