using Application.Services.Providers.Range;
using MiniExcelLibs.Attributes;

namespace BacktestRange;

enum SideType
{
    Buy = 1,
    Sell
}

record class Position()
{
    [ExcelFormat("dd/MM/yyyy HH:mm:ss.fff")]
    public required DateTime Date { get; init; }
    public required SideType Type { get; init; }
    public required double OpenPrice { get; init; }
    public double? ClosePrice { get; private set; }
    public double CurrentPrice { get; private set; }
    public double Profit { get; private set; }
    public bool IsOpen => ClosePrice is null;

    public void Update(double price, bool closePosition)
    {
        CurrentPrice = price;

        if (closePosition)
        {
            ClosePrice = price;
        }

        if (Type is SideType.Buy)
        {
            Profit = price - OpenPrice;
        }

        if (Type is SideType.Sell)
        {
            Profit = OpenPrice - price;
        }
    }
}

class Backtest(double slippage)
{
    public double Slippage { get; } = slippage;
    public List<Brick> Bricks { get; set; } = [];
    public List<Position> Positions { get; set; } = [];
    public Position? CurrentPosition
    {
        get
        {
            var last = Positions.LastOrDefault();
            if (last is null || !last.IsOpen)
            {
                return null;
            }
            return last;
        }
    }

    public void AddBrick(Brick brick)
    {
        Bricks.Add(brick);
        Statistics();
    }

    public void AddPosition(SideType type)
    {
        var lastPosition = Positions.LastOrDefault();

        if (lastPosition?.IsOpen ?? false)
        {
            throw new InvalidOperationException("Position open.");
        }

        var brick = Bricks.Last();

        Positions.Add(new()
        {
            Date = brick.Date,
            OpenPrice = type == SideType.Buy ? brick.Close + Slippage : brick.Close - Slippage,
            Type = type
        });

        Statistics();
    }

    public void ClosePosition()
    {
        var lastPosition = Positions.Last();

        if (!lastPosition.IsOpen)
        {
            throw new InvalidOperationException();
        }

        Statistics(closePosition: true);
    }

    private void Statistics(bool closePosition = false)
    {
        var lastPosition = Positions.LastOrDefault();

        if (!(lastPosition?.IsOpen ?? false))
        {
            return;
        }

        var brick = Bricks.Last();

        var closeType = lastPosition.Type == SideType.Buy ? SideType.Sell : SideType.Buy;

        lastPosition.Update(closeType == SideType.Buy ? brick.Close + Slippage : brick.Close - Slippage, closePosition);
    }
}
