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

    public const string BRICKS_KEY = "bricks";

    private readonly RangeChart _fastRangeCalculation = new(operationSettings.CurrentValue.FastBrickSize);

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

        if (_fastRangeCalculation.Bricks.Count == 0)
        {
            logger.LogInformation("Loading data from: {fromDate}", GetFromDate());
        }

        await CheckNewPrice(cancellationToken);

        state.SetCharts(BRICKS_KEY, _fastRangeCalculation);

        if (_newBricks > 0)
        {
            logger.LogInformation("NewBricks: {newBricks}", _newBricks);

            var bricks = _fastRangeCalculation.Bricks.ToArray();

            if (bricks.Length >= 3)
            {
                logger.LogInformation("Bricks[^3]: {lastBrick}", bricks[^3].LineUp);
            }

            if (bricks.Length >= 2)
            {
                logger.LogInformation("Bricks[^2]: {lastBrick}", bricks[^2].LineUp);
            }
        }
    }

    private async Task CheckNewPrice(CancellationToken cancellationToken)
    {
        var bytes = await GetNpzTicksBytesAsync(cancellationToken);

        if ((bytes?.Length ?? 0) == 0)
        {
            return;
        }

        CheckNewPrice(bytes!);
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

        var time = data[$"{MarketDataWrapper.FIELD_TIME_MSC}.npy"];
        var bid = data[$"{MarketDataWrapper.FIELD_BID}.npy"];
        var ask = data[$"{MarketDataWrapper.FIELD_ASK}.npy"];
        var last = data[$"{MarketDataWrapper.FIELD_LAST}.npy"];
        var volume = data[$"{MarketDataWrapper.FIELD_VOLUME_REAL}.npy"];
        var flags = data[$"{MarketDataWrapper.FIELD_FLAGS}.npy"];

        var tempLastTrade = default(Trade);

        _previousBricksCount = _fastRangeCalculation.Bricks.Count;

        for (var i = 0; i < time.Length - 1; i++)
        {
            var trade = Trade.Create(i, time, bid, ask, last, volume, flags);

            if (!IsNewTrade(trade))
            {
                continue;
            }

            _fastRangeCalculation.CheckNewPrice(trade.Time, trade.Last, trade.Volume);

            tempLastTrade = trade;
        }

        if (tempLastTrade is not null)
        {
            _lastTrade = tempLastTrade;
        }

        _newBricks = _fastRangeCalculation.Bricks.Count - _previousBricksCount;
    }

    private async Task<byte[]> GetNpzTicksBytesAsync(CancellationToken cancellationToken)
    {
        var fromDate = GetFromDate();
        var toDate = _currentTime.AddSeconds(AHEAD_SECONDS);

        var ticksReply = await marketDataWrapper.GetTicksRangeBytesAsync(
            operationSettings.CurrentValue.Symbol!,
            fromDate,
            toDate,
            CopyTicks.Trade,
            [MarketDataWrapper.FIELD_TIME_MSC, MarketDataWrapper.FIELD_LAST, MarketDataWrapper.FIELD_VOLUME_REAL],
            cancellationToken);

        state.CheckResponseStatus(ResponseType.GetTicks, ticksReply.ResponseStatus);

        return ticksReply.Bytes.Select(it => (byte)it).ToArray();
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
