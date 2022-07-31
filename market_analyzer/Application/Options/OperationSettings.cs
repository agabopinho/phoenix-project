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
        public decimal? Profit { get; set; }
        public string Use { get; set; } = "Atr";
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
        public KeltnerRainbowSettings KeltnerRainbow { get; set; } = new();

        public class VolatilityStopSettings
        {
            public int LookbackPeriods { get; set; } = 7;
            public double Multiplier { get; set; } = 3;
        }

        public class SlopeSettings
        {
            public int LookbackPeriods { get; set; } = 10;
        }

        public class DoubleRsiSettings
        {
            public int FastLookbackPeriods { get; set; } = 14;
            public int SlowLookbackPeriods { get; set; } = 70;
        }

        public class MacdSettings
        {
            public int FastPeriods { get; set; } = 12;
            public int SlowPeriods { get; set; } = 26;
            public int SignalPeriods { get; set; } = 9;
        }

        public class SuperTrendSettings
        {
            public int LookbackPeriods { get; set; } = 10;
            public double Multiplier { get; set; } = 3;
        }

        public class MaSettings
        {
            public int LookbackPeriods { get; set; } = 8;
        }

        public class VwapSettings
        {
            public int LookbackPeriods { get; set; } = 8;
        }

        public class KamaSettings
        {
            public int ErPeriods { get; set; } = 10;
            public int FastPeriods { get; set; } = 2;
            public int SlowPeriods { get; set; } = 30;
        }

        public class MamaSettings
        {
            public double FastLimit { get; set; } = 0.5;
            public double SlowLimit { get; set; } = 0.05;
        }

        public class T3Settings
        {
            public int LookbackPeriods { get; set; } = 5;
            public double VolumeFactor { get; set; } = 0.7;
        }

        public class AlmaSettings
        {
            public int LookbackPeriods { get; set; } = 9;
            public double Offset { get; set; } = 0.85;
            public double Sigma { get; set; } = 6;
        }

        public class KeltnerRainbowSettings
        {
            public int SmaPeriods { get; set; } = 20;
            public double Multipler { get; set; } = 0.2;
            public int AtrPeriods { get; set; } = 10;
            public double MultiplerStep { get; set; } = 0.5;
            public int Count { get; set; } = 5;
        }
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