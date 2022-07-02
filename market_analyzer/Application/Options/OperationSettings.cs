﻿namespace Application.Options
{
    public class OperationSettings
    {
        public MarketDataSettings MarketData { get; set; } = new();
        public BacktestSettings Backtest { get; set; } = new();
        public OrderSettings Order { get; set; } = new();
        public IndicatorSettings Indicator { get; set; } = new();
        public InfraSetttings Infra { get; set; } = new();
        public bool ProductionMode => Order.ExecOrder && !Backtest.Enabled;

        public class MarketDataSettings
        {
            public string? Symbol { get; set; }
            public DateOnly Date { get; set; }
            public TimeSpan Timeframe { get; set; }
            public string? TimeZoneId { get; set; }
        }

        public class BacktestSettings
        {
            public bool Enabled { get; set; }
            public TimeOnly Start { get; set; }
            public TimeOnly End { get; set; }
            public TimeSpan Step { get; set; }
        }

        public class OrderSettings
        {
            public int Deviation { get; set; }
            public long Magic { get; set; }
            public bool ExecOrder { get; set; }
        }

        public class IndicatorSettings
        {
            public TimeSpan Window { get; set; }
            public int Length { get; set; }
            public int SignalShift { get; set; }
        }

        public class InfraSetttings
        {
            public int ChunkSize { get; set; }
        }
    }
}
