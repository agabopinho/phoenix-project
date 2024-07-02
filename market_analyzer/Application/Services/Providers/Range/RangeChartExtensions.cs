using Application.Models;
using NumSharp;
using System.IO.Compression;

namespace Application.Services.Providers.Range;

public static class RangeChartExtensions
{
    public const string FIELD_TIME_MSC = "time_msc";
    public const string FIELD_BID = "bid";
    public const string FIELD_ASK = "ask";
    public const string FIELD_LAST = "last";
    public const string FIELD_VOLUME_REAL = "volume_real";
    public const string FIELD_FLAGS = "flags";

    public static Trade? CheckNewPrice(this RangeChart rangeChart, byte[] bytes, Trade? lastTrade = null, IEnumerable<Skender.Stock.Indicators.AtrResult>? atr = null)
    {
        using var bytesStream = new MemoryStream(bytes);

        return rangeChart.CheckNewPrice(bytesStream, lastTrade, atr);
    }

    public static Trade? CheckNewPrice(this RangeChart rangeChart, Stream bytesStream, Trade? lastTrade = null, IEnumerable<Skender.Stock.Indicators.AtrResult>? atr = null)
    {
        var atrValues = atr?.ToDictionary(it => it.Date, it => it.Atr);

        using var zipArchive = new ZipArchive(bytesStream);

        var data = new Dictionary<string, Array>();

        foreach (var entry in zipArchive.Entries)
        {
            using var entryStream = entry.Open();
            using var entryReader = new BinaryReader(entryStream);

            var entryBytes = entryReader.ReadBytes((int)entry.Length);

            data[entry.Name] = np.Load<Array>(entryBytes);
        }

        var time = data[$"{FIELD_TIME_MSC}.npy"];
        var bid = data[$"{FIELD_BID}.npy"];
        var ask = data[$"{FIELD_ASK}.npy"];
        var last = data[$"{FIELD_LAST}.npy"];
        var volume = data[$"{FIELD_VOLUME_REAL}.npy"];
        var flags = data[$"{FIELD_FLAGS}.npy"];

        var tempLastTrade = default(Trade);

        for (var i = 0; i < time.Length - 1; i++)
        {
            var trade = Trade.Create(i, time, bid, ask, last, volume, flags);

            if (!IsNewTrade(trade, lastTrade))
            {
                continue;
            }

            var atrKey = atrValues?.Keys.LastOrDefault(key => key < trade.Time);
            var atrValue = atrValues is not null && atrKey is not null ? atrValues[atrKey.Value] : null;

            rangeChart.CheckNewPrice(trade.Time, trade.Last, trade.Volume, atrValue);

            tempLastTrade = trade;
        }

        return tempLastTrade;
    }

    private static bool IsNewTrade(Trade trade, Trade? lastTrade)
    {
        if (trade.Time == DateTime.UnixEpoch)
        {
            return false;
        }

        if (lastTrade is null)
        {
            return true;
        }

        if (trade.Time <= lastTrade.Time)
        {
            return false;
        }

        if (trade.Time == lastTrade.Time &&
            trade.Last == lastTrade.Last &&
            trade.Flags == lastTrade.Flags &&
            trade.Volume == lastTrade.Volume)
        {
            return false;
        }

        return true;
    }
}
