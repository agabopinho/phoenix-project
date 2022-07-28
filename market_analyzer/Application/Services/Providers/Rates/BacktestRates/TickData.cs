namespace Application.Services.Providers.Rates.BacktestRates
{
    public record class TickData
    {
        public DateTime Time { get; set; }
        public double Bid { get; set; }
        public double Ask { get; set; }
        public double Last { get; set; }
        public double Volume { get; set; }
        public int Flags { get; set; }
        public double VolumeReal { get; set; }
    }
}
