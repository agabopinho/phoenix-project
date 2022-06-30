namespace Application.Options
{
    public class OperationSettings
    {
        public string? Symbol { get; set; }
        public DateOnly Date { get; set; }
        public int ChunkSize { get; set; }
        public TimeSpan Timeframe { get; set; }
        public int Deviation { get; set; }
        public long Magic { get; set; }
        public bool ExecOrder { get; set; }
        public TimeSpan IndicatorWindow { get; set; }
        public int IndicatorLength { get; set; }
        public int IndicatorSignalShift { get; set; }
    }
}
