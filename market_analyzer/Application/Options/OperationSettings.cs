namespace Application.Options
{
    public class OperationSettings
    {
        public string? Symbol { get; set; }
        public DateOnly Date { get; set; }
        public int ChunkSize { get; set; }
        public TimeSpan Timeframe { get; set; }
        public TimeSpan Window { get; set; }
        public int Deviation { get; set; }
        public long Magic { get; set; }
        public bool ExecOrder { get; set; }
    }
}
