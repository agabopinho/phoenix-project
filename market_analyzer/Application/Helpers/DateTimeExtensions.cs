namespace Application.Helpers
{
    public static class DateTimeExtensions
    {
        public static readonly TimeZoneInfo DefaultTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");

        public static double ToTimestamp(this DateTime dateTime)
            => (dateTime - DateTime.UnixEpoch).TotalSeconds;
    }
}
