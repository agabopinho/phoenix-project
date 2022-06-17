namespace Application.Services
{
    public static class DateTimeExtensions
    {
        public static DateTime ToDateTime(this double timestamp)
            => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UnixEpoch.AddSeconds(timestamp), Defaults.DefaultTimeZone);

        public static double ToTimestamp(this DateTime dateTime)
            => (dateTime - DateTime.UnixEpoch).TotalSeconds;
    }
}
