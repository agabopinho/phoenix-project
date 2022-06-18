namespace Application.Constants
{
    public static class Defaults
    {
        public static readonly TimeZoneInfo DefaultTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        public static int DefaultListCapacity { get; set; } = 3000;
    }
}
