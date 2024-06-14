namespace Application.Models;

public class Trade(DateTime time, double bid, double ask, double last, double volume, double flags)
{
    public DateTime Time { get; } = time;
    public double Bid { get; } = bid;
    public double Ask { get; } = ask;
    public double Last { get; } = last;
    public double Volume { get; } = volume;
    public double Flags { get; } = flags;

    public static Trade Create(object time, object bid, object ask, object last, object volume, object flags)
    {
        return new Trade(
            DateTime.UnixEpoch.AddMilliseconds((long)time),
            (double)bid!,
            (double)ask!,
            (double)last!,
            (double)volume!,
            (uint)flags);
    }

    public static Trade Create(int index, Array time, Array bid, Array ask, Array last, Array volume, Array flags)
    {
        var timeValue = index <= time.Length - 1 ? time.GetValue(index)! : DateTime.UnixEpoch;
        var bidValue = index <= bid.Length - 1 ? bid.GetValue(index)! : 0D;
        var askValue = index <= ask.Length - 1 ? ask.GetValue(index)! : 0D;
        var lastValue = index <= last.Length - 1 ? last.GetValue(index)! : 0D;
        var volumeValue = index <= volume.Length - 1 ? volume.GetValue(index)! : 0D;
        var flagsValue = index <= flags.Length - 1 ? flags.GetValue(index)! : 0U;

        return Create(
            timeValue,
            bidValue,
            askValue,
            lastValue,
            volumeValue,
            flagsValue);
    }
}