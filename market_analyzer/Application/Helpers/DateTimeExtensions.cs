namespace Application.Helpers
{
    public static class DateTimeExtensions
    {
        public static double ToTimestamp(this DateTime dateTime)
            => (dateTime - DateTime.UnixEpoch).TotalSeconds;
    }
}
