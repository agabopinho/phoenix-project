namespace Application.Models;

public class SimpleTrade(DateTime time, double bid, double ask, double last, double volume, double flags)
{
    public DateTime Time { get; } = time;
    public double Bid { get; } = bid;
    public double Ask { get; } = ask;
    public double Last { get; } = last;
    public double Volume { get; } = volume;
    public double Flags { get; } = flags;

    public static SimpleTrade Create(object time, object bid, object ask, object last, object volume, object flags)
    {
        return new SimpleTrade(
            DateTime.UnixEpoch.AddMilliseconds((long)time),
            (double)bid!,
            (double)ask!,
            (double)last!,
            (ulong)volume!,
            (uint)flags);
    }
}