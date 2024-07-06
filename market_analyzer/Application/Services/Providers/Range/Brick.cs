using MiniExcelLibs.Attributes;
using System.Diagnostics;

namespace Application.Services.Providers.Range;

public record class Brick
{
    [ExcelFormat("dd/MM/yyyy HH:mm:ss.fff")]
    public DateTime Date { get; set; }
    public BrickType Type { get; set; } = BrickType.Last;
    public double Open { get; set; }
    public double High { get; set; }
    public double Low { get; set; }
    public double Close { get; set; }
    public int TicksCount { get; set; }
    public double Volume { get; set; }

    [ExcelIgnore]
    public double[] Ohlc => [Open, High, Low, Close];

    public double LineUp => Math.Max(Open, Close);
    public double LineDown => Math.Min(Open, Close);
}

