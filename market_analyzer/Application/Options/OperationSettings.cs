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
        public string Use { get; set; } = "VolatilityStop";
        public VolatilityStopSettings VolatilityStop { get; set; } = new();
        public SlopeSettings Slope { get; set; } = new();
        public DoubleRsiSettings DoubleRsi { get; set; } = new();
        public MacdSettings Macd { get; set; } = new();
        public SuperTrendSettings SuperTrend { get; set; } = new();
        public MaSettings Ma { get; set; } = new();
        public VwapSettings Vwap { get; set; } = new();
        public KamaSettings Kama { get; set; } = new();
        public MamaSettings Mama { get; set; } = new();
        public T3Settings T3 { get; set; } = new();
        public AlmaSettings Alma { get; set; } = new();
        public KeltnerAndEmaSignalSettings KeltnerAndEmaSignal { get; set; } = new();
        public MiniBovespaSettings MiniBovespa { get; set; } = new();
        public RenkoSettings Renko { get; set; } = new();
        public RenkoAtrSettings RenkoAtr { get; set; } = new();
        public TrendSettings Trend { get; set; } = new();
        public KeltnerRainbowSettings KeltnerRainbow { get; set; } = new();
        public VolatilityStopRainbowSettings VolatilityStopRainbow { get; set; } = new();

        public record class Risk(
            double? TakeProfit = null,
            double? StopLoss = null);

        public record class VolatilityStopSettings(
            int LookbackPeriods = 7,
            double Multiplier = 3);

        public record class SlopeSettings(
            int LookbackPeriods = 10);

        public record class DoubleRsiSettings(
            int FastLookbackPeriods = 14,
            int SlowLookbackPeriods = 70);

        public record class MacdSettings(
            int FastPeriods = 12,
            int SlowPeriods = 26,
            int SignalPeriods = 9);

        public record class SuperTrendSettings(
            int LookbackPeriods = 10,
            double Multiplier = 3);

        public record class MaSettings(
             int LookbackPeriods = 8);

        public record class VwapSettings(
            int LookbackPeriods = 8);

        public record class KamaSettings(
            int ErPeriods = 10,
            int FastPeriods = 2,
            int SlowPeriods = 30);

        public record class MamaSettings(
            double FastLimit = 0.5,
            double SlowLimit = 0.05);

        public record class T3Settings(
            int LookbackPeriods = 5,
            double VolumeFactor = 0.7);

        public record class AlmaSettings(
            int LookbackPeriods = 9,
            double Offset = 0.85,
            double Sigma = 6);

        public record class KeltnerAndEmaSignalSettings(
            int EmaLookbackPeriods = 9,
            int EmaPeriods = 20,
            double Multipler = 0.2,
            int AtrPeriods = 10);

        public record class MiniBovespaSettings(
            string Use = "Ema",
            double StartHighP = 0.5,
            double StartLowP = 0.5,
            double MinHighP = 0.25,
            double MinLowP = 0.25);

        public record class RenkoSettings(
            string Use = "Ema",
            double BrickSize = 10,
            bool FireOnlyAtCandleOpening = true);

        public record class RenkoAtrSettings(
            string Use = "Ema",
            int AtrPeriods = 10,
            bool FireOnlyAtCandleOpening = true);

        public record class TrendSettings(
            string Use = "Ema",
            int MaxPower = 5);

        public record class KeltnerRainbowSettings(
            string Use = "Ema",
            int EmaPeriods = 20,
            double Multipler = 1,
            int AtrPeriods = 10,
            double MultiplerStep = 1,
            int Count = 5);

        public record class VolatilityStopRainbowSettings(
            string Use = "Ema",
            int LookbackPeriods = 20,
            double Multipler = 0.2,
            double MultiplerStep = 0.5,
            int Count = 5);
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