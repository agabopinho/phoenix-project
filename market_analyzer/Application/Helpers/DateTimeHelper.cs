namespace Application.Helpers
{
    public static class DateTimeHelper
    {
        public static readonly TimeZoneInfo LocalTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

        public static DateTime LocalNow()
           => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, LocalTimeZone);
    }
}
