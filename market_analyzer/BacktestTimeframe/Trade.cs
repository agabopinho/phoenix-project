using OoplesFinance.StockIndicators.Enums;

namespace Backtesting;

public record class Trade
{
    public string? Date { get; set; }
    public double Open { get; set; }
    public double High { get; set; }
    public double Low { get; set; }
    public double Close { get; set; }
    public double Volume { get; set; }
    public Signal? Signal { get; set; }
    public double? Price { get; set; }
    public double Profit { get; set; }
}