namespace Application.Options;

public class OperationSettings
{
    public DateOnly Date { get; set; }
    public TimeOnly Start { get; set; }
    public TimeOnly End { get; set; }
    public TimeSpan Timeframe { get; set; }
    public string? TimeZoneId { get; set; }
    public SymbolSettings Symbol { get; set; } = new();
    public OrderSettings Order { get; set; } = new();
    public StreamingSettings StreamingData { get; set; } = new();
    public bool ProductionMode => Order.ExecOrder;
}

public class SymbolSettings
{
    public string? Name { get; set; }
}

public class OrderSettings
{
    public int Deviation { get; set; }
    public long Magic { get; set; }
    public bool ExecOrder { get; set; }
}

public class StreamingSettings
{
    public int ChunkSize { get; set; }
}
