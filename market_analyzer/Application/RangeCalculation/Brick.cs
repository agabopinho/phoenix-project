﻿namespace Application.Range;

public record class Brick
{
    public DateTime Date { get; set; }
    public BrickType Type { get; set; } = BrickType.Last;
    public double Open { get; set; }
    public double High { get; set; }
    public double Low { get; set; }
    public double Close { get; set; }
    public int TicksCount { get; set; }
    public double Volume { get; set; }
}

