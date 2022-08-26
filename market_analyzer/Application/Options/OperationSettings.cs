using Skender.Stock.Indicators;

namespace Application.Options
{
    public class OperationSettings
    {
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
    }

    public class StrategySettings
    {
        public DateOnly Date { get; set; }
        public TimeOnly Start { get; set; }
        public TimeOnly End { get; set; }
        public TimeSpan Timeframe { get; set; }
        public string? TimeZoneId { get; set; }
        public bool FireOnlyAtCandleOpening { get; set; }
        public TimeSpan? PeriodicTimer { get; set; }
        public double Volume { get; set; }
        public Risk OperationRisk { get; set; } = new();
        public Risk DailyRisk { get; set; } = new();
        public string Use { get; set; } = "Hope";
        public RenkoSettings Renko { get; set; } = new();
        public RenkoAtrSettings RenkoAtr { get; set; } = new();
        public Dictionary<string, object> Hope { get; set; } = new();

        public record class Risk(
            double? TakeProfit = null,
            double? StopLoss = null);

        public record class RenkoSettings(
            string Use = "Ema",
            double BrickSize = 10,
            bool FireOnlyAtCandleOpening = true);

        public record class RenkoAtrSettings(
            string Use = "Ema",
            int AtrPeriods = 10,
            bool FireOnlyAtCandleOpening = true);
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