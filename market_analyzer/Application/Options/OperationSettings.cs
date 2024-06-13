namespace Application.Options;

public class OperationSettings
{
    public string? TimeZoneId { get; set; }
    public string? Symbol { get; set; }
    public double? BrickSize { get; set; }
    public OrderSettings Order { get; set; } = new();
}

public class OrderSettings
{
    public int Deviation { get; set; }
    public long Magic { get; set; }
}