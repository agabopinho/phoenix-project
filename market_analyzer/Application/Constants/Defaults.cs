namespace Application.Constants
{
    public static class Defaults
    {
        public static readonly TimeZoneInfo DefaultTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        public static int DefaultListCapacity { get; set; } = 3000;
        public static string Symbol { get; set; } = "WINQ22";
        public static TimeSpan SlidingTime { get; set; } = TimeSpan.FromSeconds(180);
    }
}
