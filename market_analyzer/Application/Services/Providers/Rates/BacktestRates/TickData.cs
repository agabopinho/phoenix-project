namespace Application.Services.Providers.Rates.BacktestRates
{
    public class TickData
    {
        public int Id { get; set; }
        public DateTime Time { get; set; }
        public double Bid { get; set; }
        public double Ask { get; set; }
        public double Last { get; set; }
        public double Volume { get; set; }
        public int Flags { get; set; }
        public double VolumeReal { get; set; }
    }
}
