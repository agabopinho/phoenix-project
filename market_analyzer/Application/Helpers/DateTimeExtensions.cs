namespace Application.Helpers;

public static class DateTimeExtensions
{
    public static double ToUnixEpochMilliseconds(this DateTime dateTime)
    {
        return (dateTime - DateTime.UnixEpoch).TotalMilliseconds;
    }

    public static double ToUnixEpochSeconds(this DateTime dateTime)
    {
        return (dateTime - DateTime.UnixEpoch).TotalSeconds;
    }

    public static DateTime DateTimeFromUnixEpochMilliseconds(this double value)
    {
        return DateTime.UnixEpoch.AddMilliseconds(value);
    }

    public static DateTime DateTimeFromUnixEpochSeconds(this double value)
    {
        return DateTime.UnixEpoch.AddSeconds(value);
    }
}
