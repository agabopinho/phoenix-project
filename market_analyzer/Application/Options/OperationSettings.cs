namespace Application.Options
{
    public class OperationSettings
    {
        public DateOnly Date { get; set; }
        public TimeOnly Start { get; set; }
        public TimeOnly End { get; set; }
        public TimeSpan Timeframe { get; set; }
        public TimeSpan Window { get; set; }
        public string? TimeZoneId { get; set; }
        public SymbolSettings Symbol { get; set; } = new();
        public StrategySettings Strategy { get; set; } = new();
        public BacktestSettings Backtest { get; set; } = new();
        public OrderSettings Order { get; set; } = new();
        public StreamingSettings StreamingData { get; set; } = new();
        public bool ProductionMode => Order.ExecOrder && !Backtest.Enabled;
    }

    public class SymbolSettings
    {
        public string? Name { get; set; }
        public int PriceDecimals { get; set; }
        public int VolumeDecimals { get; set; }
        public decimal StandardLot { get; set; }
    }

    public class StrategySettings
    {
        public decimal Volume { get; set; }
        public decimal IncrementVolume { get; set; }
        public int AtrLookbackPeriods { get; set; }
        public double AtrMultiplier { get; set; }
        public decimal? Profit { get; set; }
    }

    public class BacktestSettings
    {
        public bool Enabled { get; set; }
        public TimeSpan Step { get; set; }
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
}
