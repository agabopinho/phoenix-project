using Grpc.Terminal;
using OoplesFinance.StockIndicators.Models;

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
                }).ToArray();
    }
}
