using Application.Services.Providers.Range;
using OoplesFinance.StockIndicators.Enums;

namespace Backtesting;

public record class Trade
{
    public string? Date { get; set; }
    public BrickType Type { get; set; } = BrickType.Last;
    public double Open { get; set; }
    public double High { get; set; }
    public double Low { get; set; }
    public double Close { get; set; }
    public int TicksCount { get; set; }
    public double Volume { get; set; }
    public Signal? OriginalSignal { get; set; }
    public Signal? Signal { get; set; }
    public double? Price { get; set; }
    public double Profit { get; set; }
}