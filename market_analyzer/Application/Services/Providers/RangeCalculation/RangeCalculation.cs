namespace Application.Services.Providers.RangeCalculation;

public class RangeCalculation
{
    private readonly List<Brick> _bricks = new(1000);
    private double _brickSize;

    public RangeCalculation(double brickSize, List<DateTime>? times = null, List<double>? prices = null, List<double>? volume = null)
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
            _brickSize = (double)brickSize;
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

            var values = new[] { last.Open, last.High, last.Low, last.Close };

            last.High = values.Max();
            last.Low = values.Min();
            last.TicksCount += 1;
            last.Volume += volume;
        }

        if (last.Type is BrickType.Up or BrickType.Last)
        {
            if (price > last.Open)
            {
                delta = price - last.Open;
                bricksCount = (int)Math.Floor(delta / _brickSize);
                if (bricksCount > 0)
                {
                    AddBricks(BrickType.Up, time, price, volume, bricksCount);
                }
            }
            else if (price <= last.Open)
            {
                delta = last.Open - price;
                bricksCount = (int)Math.Floor(delta / _brickSize);
                if (bricksCount > 0)
                {
                    AddBricks(BrickType.Down, time, price, volume, bricksCount);
                }
            }
            return;
        }

        if (last.Type is BrickType.Down)
        {
            if (price < last.Open)
            {
                delta = last.Open - price;
                bricksCount = (int)Math.Floor(delta / _brickSize);
                if (bricksCount > 0)
                {
                    AddBricks(BrickType.Down, time, price, volume, bricksCount);
                }
            }
            else if (price >= last.Open)
            {
                delta = price - last.Open;
                bricksCount = (int)Math.Floor(delta / _brickSize);
                if (bricksCount > 0)
                {
                    AddBricks(BrickType.Up, time, price, volume, bricksCount);
                }
            }
            return;
        }
    }

    private void AddBricks(BrickType type, DateTime time, double price, double volume, int count)
    {
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
}

