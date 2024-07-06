using Application.Models;
using Application.Services.Providers.Range;
using Infrastructure.GrpcServerTerminal;
using NumSharp;
using System.IO.Compression;

namespace BacktestRange;

public static class RangeChartExtensions
{
    public static Trade? CheckNewPrice(this RangeChart rangeChart, byte[] bytes, Trade? lastTrade = null)
    {
        using var bytesStream = new MemoryStream(bytes);

        return rangeChart.CheckNewPrice(bytesStream, lastTrade);
    }

    public static Trade? CheckNewPrice(this RangeChart rangeChart, Stream bytesStream, Trade? lastTrade = null)
    {
        using var zipArchive = new ZipArchive(bytesStream);

        var data = new Dictionary<string, Array>();

        foreach (var entry in zipArchive.Entries)
        {
            using var entryStream = entry.Open();
            using var entryReader = new BinaryReader(entryStream);

            var entryBytes = entryReader.ReadBytes((int)entry.Length);

            data[entry.Name] = np.Load<Array>(entryBytes);
        }

        var time = data[$"{MarketDataWrapper.FIELD_TIME_MSC}.npy"];
        var bid = data[$"{MarketDataWrapper.FIELD_BID}.npy"];
        var ask = data[$"{MarketDataWrapper.FIELD_ASK}.npy"];
        var last = data[$"{MarketDataWrapper.FIELD_LAST}.npy"];
        var volume = data[$"{MarketDataWrapper.FIELD_VOLUME_REAL}.npy"];
        var flags = data[$"{MarketDataWrapper.FIELD_FLAGS}.npy"];

        var tempLastTrade = default(Trade);

        for (var i = 0; i < time.Length - 1; i++)
        {
            var trade = Trade.Create(i, time, bid, ask, last, volume, flags);

            rangeChart.CheckNewPrice(trade.Time, trade.Last, trade.Volume);

            tempLastTrade = trade;
        }

        return tempLastTrade;
    }
}
