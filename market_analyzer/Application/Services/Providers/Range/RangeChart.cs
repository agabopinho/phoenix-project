namespace Application.Services.Providers.Range;

public class RangeChart
{
    private readonly List<Brick> _bricks = new(10_000);
    private double _brickSize;

    public RangeChart(double brickSize, List<DateTime>? times = null, List<double>? prices = null, List<double>? volume = null)
    {
        _brickSize = brickSize;

        if (times is not null && prices is not null && volume is not null)
        {
            for (var i = 0; i < times.Count; i++)
            {
                CheckNewPrice(times[i], prices[i], volume[i]);
            }
        }
    }

    public IReadOnlyCollection<Brick> Bricks => _bricks;

    public void CheckNewPrice(DateTime time, double price, double volume, double? brickSize = null)
    {
        if (brickSize != null)
        {
            _brickSize = brickSize.Value;
        }

        if (_bricks.Count == 0)
        {
            _bricks.Add(new Brick
            {
                Date = time,
                Type = BrickType.Last,
                Open = price,
                High = price,
                Low = price,
                Close = price,
                TicksCount = 1,
                Volume = volume
            });

            return;
        }

        var last = _bricks.Last();

        var delta = Math.Abs(price - last.Open);
        var bricksCount = (int)Math.Floor(delta / _brickSize);

        if (bricksCount == 0)
        {
            last.Close = price;
            last.TicksCount += 1;
            last.Volume += volume;

            var ohlc = last.Ohlc;

            last.High = ohlc.Max();
            last.Low = ohlc.Min();

            return;
        }

        if (price > last.Open)
        {
            AddBricks(BrickType.Up, time, price, volume, bricksCount);
        }
        else
        {
            AddBricks(BrickType.Down, time, price, volume, bricksCount);
        }
    }

    private void AddBricks(BrickType type, DateTime time, double price, double volume, int count)
    {
        if (type is not (BrickType.Up or BrickType.Down))
        {
            throw new ArgumentException($"Invalid add {nameof(BrickType)}: {type}.");
        }

        var last = _bricks.Last();

        if (last.Type is BrickType.Last)
        {
            if (type is BrickType.Up)
            {
                last.Type = type;
                last.Close = last.Open + _brickSize;
                last.High = last.Open + _brickSize;
            }
            else if (type is BrickType.Down)
            {
                last.Type = type;
                last.Close = last.Open - _brickSize;
                last.Low = last.Open - _brickSize;
            }
        }

        for (var i = 1; i < count; i++)
        {
            last = _bricks.Last();

            if (type is BrickType.Up)
            {
                _bricks.Add(new Brick
                {
                    Date = time,
                    Type = type,
                    Open = last.Close,
                    High = last.Close + _brickSize,
                    Low = last.Close,
                    Close = last.Close + _brickSize,
                    TicksCount = 0,
                    Volume = 0
                });
            }
            else if (type is BrickType.Down)
            {
                _bricks.Add(new Brick
                {
                    Date = time,
                    Type = type,
                    Open = last.Close,
                    High = last.Close,
                    Low = last.Close - _brickSize,
                    Close = last.Close - _brickSize,
                    TicksCount = 0,
                    Volume = 0
                });
            }
        }

        last = _bricks.Last();

        var newItem = new Brick
        {
            Date = time,
            Type = BrickType.Last,
            Open = last.Close,
            High = Math.Max(price, last.Close),
            Low = Math.Min(price, last.Close),
            Close = price,
            TicksCount = 1,
            Volume = volume
        };

        _bricks.Add(newItem);
    }

    public IReadOnlyCollection<Brick> GetUniqueBricks()
    {
        var bricks = _bricks.ToArray();
        var bricksList = new List<Brick>();
        var lastBrick = default(Brick);

        foreach (var brick in bricks[..^1])
        {
            if (lastBrick?.LineUp == brick.LineUp)
            {
                continue;
            }

            bricksList.Add(brick);
            lastBrick = brick;
        }

        return [.. bricksList];
    }
}

