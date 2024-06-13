namespace Infrastructure.GrpcServerTerminal;

public class GrpcServerOptions
{
    public Dictionary<string, string>? MarketData { get; set; }
    public Dictionary<string, string>? OrderManagement { get; set; }
}
