namespace Application.Options
{
    public class OperationSettings
    {
        public TimeOnly Start { get; set; }
        public TimeOnly End { get; set; }
        public SymbolDataSettings SymbolData { get; set; } = new();
        public StrategyDataSettings StrategyData { get; set; } = new();
        public BacktestSettings Backtest { get; set; } = new();
        public OrderSettings Order { get; set; } = new();
        public bool ProductionMode => Order.ExecOrder && !Backtest.Enabled;

        public class SymbolDataSettings
        {
            public string? Name { get; set; }
            public int PriceDecimals { get; set; }
            public int VolumeDecimals { get; set; }
            public decimal StandardLot { get; set; }
            public DateOnly Date { get; set; }
            public TimeSpan Timeframe { get; set; }
            public TimeSpan Window { get; set; }
            public string? TimeZoneId { get; set; }
            public int ChunkSize { get; set; }
        }

        public class StrategyDataSettings
        {
            public decimal RangePoints { get; set; }
            public decimal Volume { get; set; }
            public decimal MoreVolumeFactor { get; set; }
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
    }
}
