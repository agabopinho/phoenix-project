namespace Application.Services
{
    public static class DateTimeExtensions
    {
        public static DateTime ToDateTime(this double timestamp)
            => DateTime.SpecifyKind(DateTime.UnixEpoch.AddSeconds(timestamp), DateTimeKind.Unspecified);

        public static double ToTimestamp(this DateTime dateTime)
            => (dateTime - DateTime.UnixEpoch).TotalSeconds;
    }
}
