using Grpc.Terminal;
using OoplesFinance.StockIndicators.Models;

namespace Application.Helpers;

public static class RatesExtensions
{
    public static IEnumerable<TickerData> ToTickerData(this IEnumerable<Rate> rates)
        => rates
            .Where(it => it.Open.HasValue && !double.IsNaN(it.Open.Value))
            .Select(it => new TickerData
            {
                Date = it.Time.ToDateTime(),
                Open = it.Open ?? 0,
                High = it.High ?? 0,
                Low = it.Low ?? 0,
                Close = it.Close ?? 0,
                Volume = it.Volume ?? 0,
            })
            .OrderBy(it => it.Date);
}
