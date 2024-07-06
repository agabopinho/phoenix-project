namespace Application.Options;

public class OperationOptions
{
    public string? TimeZoneId { get; set; }
    public string? Symbol { get; set; }
    public double FastBrickSize { get; set; }
    public DateTime? ResumeFrom { get; set; }
    public OrderOptions Order { get; set; } = new();
    public SanityTestOptions SanityTest { get; set; } = new();
}

public class OrderOptions
{
    public long Magic { get; set; }
    public int Deviation { get; set; }
    public double Lot { get; set; }
    public double TakeProfitPips { get; set; }
    public double StopLossPips { get; set; }
    public ProductionMode ProductionMode { get; set; }
    public double MaximumInformationDelay { get; set; }
    public int WhileDelay { get; set; }
    public long AwaitTimeout { get; set; }
}

public class SanityTestOptions
{
    public long Magic { get; set; }
    public double Lot { get; set; }
    public double PipsRange { get; set; }
    public double PipsStep { get; set; }
    public int OrderModifications { get; set; }
    public bool Execute { get; set; }
}

public enum ProductionMode
{
    Off = 0,
    On,
}