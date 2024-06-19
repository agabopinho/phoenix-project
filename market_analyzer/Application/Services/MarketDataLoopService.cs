using Application.Models;
using Application.Options;
using Application.Services.Providers.Date;
using Application.Services.Providers.Range;
using Grpc.Terminal.Enums;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NumSharp;
using System.IO.Compression;

namespace Application.Services;

public class MarketDataLoopService(
    IMarketDataWrapper marketDataWrapper,
    IDate dateProvider,
    State state,
    IOptionsMonitor<OperationOptions> operationSettings,
    ILogger<MarketDataLoopService> logger
) : ILoopService
{
    private const int AHEAD_SECONDS = 30;

    private const string FIELD_TIME_MSC = "time_msc";
    private const string FIELD_BID = "bid";
    private const string FIELD_ASK = "ask";
    private const string FIELD_LAST = "last";
    private const string FIELD_VOLUME_REAL = "volume_real";
    private const string FIELD_FLAGS = "flags";

    private readonly RangeChart _rangeCalculation = new(operationSettings.CurrentValue.BrickSize);

    private DateTime _currentTime;
    private Trade? _lastTrade;
    private int _previousBricksCount;
    private int _newBricks;

    private void PreExecution()
    {
        _currentTime = dateProvider.LocalDateSpecifiedUtcKind();
    }

    public Task<bool> StoppedAsync(CancellationToken stoppingToken)
    {
        return Task.FromResult(false);
    }

    public Task<bool> CanRunAsync(CancellationToken stoppingToken)
    {
        return Task.FromResult(true);
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        PreExecution();

        await CheckNewPrice(cancellationToken);

        state.SetBricks([.. _rangeCalculation.Bricks], _lastTrade);

        if (_newBricks > 0)
        {
            logger.LogInformation("NewBricks: {newBricks}", _newBricks);
        }
    }

    private async Task CheckNewPrice(CancellationToken cancellationToken)
    {
        _previousBricksCount = _rangeCalculation.Bricks.Count;

        var bytesAsInt = await GetNpzTicksStreamAsync(cancellationToken);

        if (bytesAsInt is null || !bytesAsInt.Any())
        {
            return;
        }

        var bytes = bytesAsInt.Select(it => (byte)it).ToArray();

        CheckNewPrice(bytes);
    }

    private void CheckNewPrice(byte[] bytes)
    {
        using var bytesStream = new MemoryStream(bytes);

        CheckNewPrice(bytesStream);
    }

    private void CheckNewPrice(Stream bytesStream)
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

            if (!IsNewTrade(trade))
            {
                continue;
            }

            _rangeCalculation.CheckNewPrice(trade.Time, trade.Last, trade.Volume);

            tempLastTrade = trade;
        }

        if (tempLastTrade is not null)
        {
            _lastTrade = tempLastTrade;
        }

        _newBricks = _rangeCalculation.Bricks.Count - _previousBricksCount;
    }

    private async Task<IEnumerable<int>> GetNpzTicksStreamAsync(CancellationToken cancellationToken)
    {
        var fromDate = GetFromDate();
        var toDate = _currentTime.AddSeconds(AHEAD_SECONDS);

        if (_previousBricksCount == 0)
        {
            logger.LogInformation("Loading data from: {fromDate}", fromDate);
        }

        var ticksReply = await marketDataWrapper.GetTicksRangeBytesAsync(
            operationSettings.CurrentValue.Symbol!,
            fromDate,
            toDate,
            CopyTicks.Trade,
            [FIELD_TIME_MSC, FIELD_LAST],
            cancellationToken);

        state.CheckResponseStatus(ResponseType.GetTicks, ticksReply.ResponseStatus);

        return ticksReply.Bytes;
    }

    private DateTime GetFromDate()
    {
        if (_lastTrade?.Time is not null)
        {
            return _lastTrade.Time;
        }

        var resumeFrom = operationSettings.CurrentValue.ResumeFrom;

        if (resumeFrom is not null)
        {
            return DateTime.SpecifyKind(resumeFrom.Value, DateTimeKind.Utc);
        }

        return _currentTime - _currentTime.TimeOfDay;
    }

    private bool IsNewTrade(Trade trade)
    {
        if (trade.Time == DateTime.UnixEpoch)
        {
            return false;
        }

        if (_lastTrade is null)
        {
            return true;
        }

        if (trade.Time <= _lastTrade.Time)
        {
            return false;
        }

        if (trade.Time == _lastTrade.Time &&
            trade.Last == _lastTrade.Last &&
            trade.Flags == _lastTrade.Flags &&
            trade.Volume == _lastTrade.Volume)
        {
            return false;
        }

        return true;
    }
}
