namespace Application.Helpers;

public static class DateTimeExtensions
{
    public static double ToUnixEpochTimestamp(this DateTime dateTime)
        => (dateTime - DateTime.UnixEpoch).TotalSeconds;
}
