using Application.Helpers;
using Skender.Stock.Indicators;

namespace Application.Objects
{
    public class CustomQuote : IQuote
    {
        public CustomQuote()
        {
        }

        public CustomQuote(double index, decimal[] values)
        {
            Date = index.ToDateTime();
            Open = values[0];
            High = values[1];
            Low = values[2];
            Close = values[3];
            Volume = 0;
        }

        public DateTime Date { get; set; }
        public decimal Open { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Close { get; set; }
        public decimal Volume { get; set; }
    }
}
