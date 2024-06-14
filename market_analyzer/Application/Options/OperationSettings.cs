﻿namespace Application.Options;

public class OperationOptions
{
    public string? TimeZoneId { get; set; }
    public string? Symbol { get; set; }
    public double? BrickSize { get; set; }
    public OrderOptions Order { get; set; } = new();
    public SanityTestOptions SanityTest { get; set; } = new();
}

public class OrderOptions
{
    public double Lot { get; set; }
    public int Deviation { get; set; }
    public long Magic { get; set; }
}

public class SanityTestOptions
{
    public double Lot { get; set; }
    public double Pips { get; set; }
    public bool Execute { get; set; }
}