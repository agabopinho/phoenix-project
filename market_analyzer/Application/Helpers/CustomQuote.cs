using Skender.Stock.Indicators;

namespace Application.Helpers;

public record class CustomQuote : IQuote
{
    public DateTime Date { get; set; }

    public decimal Open { get; set; }

    public decimal High { get; set; }

    public decimal Low { get; set; }

    public decimal Close { get; set; }

    public decimal Volume { get; set; }
}
