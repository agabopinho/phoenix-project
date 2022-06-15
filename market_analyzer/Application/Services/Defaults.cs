namespace Application.Services
{
    public static class Defaults
    {
        public static readonly TimeZoneInfo DefaultTimeZone = TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
        public static int DefaultRateListCapacity { get; set; } = 3000;
    }
}
