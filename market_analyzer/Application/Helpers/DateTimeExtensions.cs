using Application.Constants;

namespace Application.Helpers
{
    public static class DateTimeExtensions
    {
        public static DateTime ToDateTime(this double timestamp)
        {
            var utcDate = DateTime.UnixEpoch.AddSeconds(timestamp);
            var defaultTimeZoneDate = TimeZoneInfo.ConvertTimeFromUtc(utcDate, Defaults.DefaultTimeZone);

            return defaultTimeZoneDate;
        }

        public static double ToTimestamp(this DateTime dateTime)
            => (dateTime - DateTime.UnixEpoch).TotalSeconds;
    }
}
