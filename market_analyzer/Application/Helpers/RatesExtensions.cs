using Grpc.Terminal;
using OoplesFinance.StockIndicators.Models;
using Skender.Stock.Indicators;

namespace Application.Helpers
{
    public static class RatesExtensions
    {
        public static IEnumerable<TickerData> ToTickerData(this IEnumerable<Rate> rates)
            => rates
                .Where(it => it.Open.HasValue && !double.IsNaN(it.Open.Value))
                .Select(it => new TickerData
                {
                    Date = it.Time.ToDateTime(),
                    Open = Convert.ToDecimal(it.Open),
                    High = Convert.ToDecimal(it.High),
                    Low = Convert.ToDecimal(it.Low),
                    Close = Convert.ToDecimal(it.Close),
                    Volume = Convert.ToDecimal(it.Volume),
                })
                .OrderBy(it => it.Date);

        public static IEnumerable<IQuote> ToQuotes(this IEnumerable<Rate> rates)
            => rates
                .Where(it => it.Open.HasValue && !double.IsNaN(it.Open.Value))
                .Select(it => new CustomQuote
                {
                    Date = it.Time.ToDateTime(),
                    Open = Convert.ToDecimal(it.Open),
                    High = Convert.ToDecimal(it.High),
                    Low = Convert.ToDecimal(it.Low),
                    Close = Convert.ToDecimal(it.Close),
                    Volume = Convert.ToDecimal(it.Volume),
                })
                .OrderBy(it => it.Date);
    }

    public record class CustomQuote : IQuote
    {
        public DateTime Date { get; set; }

        public decimal Open { get; set; }

        public decimal High { get; set; }

        public decimal Low { get; set; }

        public decimal Close { get; set; }

        public decimal Volume { get; set; }
    }
}
