using Application.Helpers;

namespace Application.Objects
{
    public class Rate
    {
        public Rate()
        {
        }

        public Rate(double[] values)
        {
            Time = values[0].ToDateTime();
            Open = values[1];
            High = values[2];
            Low = values[3];
            Close = values[4];
            TickVolume = values[5];
            Spread = values[6];
            RealVolume = values[7];
        }

        public DateTime Time { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public double TickVolume { get; set; }
        public double Spread { get; set; }
        public double RealVolume { get; set; }
    }
}
