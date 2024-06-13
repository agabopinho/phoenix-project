using Application.Models;
using Application.Options;
using Application.Range;
using Application.Services.Providers.Date;
using Grpc.Terminal.Enums;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NumSharp;

namespace Application.Services;

public class MarketDataLoopService(
    IMarketDataWrapper marketDataWrapper,
    IDateProvider dateProvider,
    State state,
    IOptionsMonitor<OperationSettings> operationSettings,
    ILogger<ILoopService> logger) : ILoopService
{
    private const int AHEAD_SECONDS = 30;

    private readonly RangeCalculation _rangeCalculation = new(operationSettings.CurrentValue.BrickSize!.Value);

    private DateTime _currentTime;
    private SimpleTrade? _lastTrade;
    private int _previousBricksCount;
    private int _newBricks;

    private void PreExecution()
    {
        _currentTime = dateProvider.LocalDateSpecifiedUtcKind();
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        PreExecution();

        state.Bricks = _rangeCalculation.Bricks;

        await CheckNewPrice(cancellationToken);

        state.LastTradeTime = _lastTrade?.Time;

        if (_newBricks > 0)
        {
            logger.LogInformation("NewBricks: {newBricks}", _newBricks);
        }
    }

    private async Task CheckNewPrice(CancellationToken cancellationToken)
    {
        _previousBricksCount = _rangeCalculation.Bricks.Count;

        var bytes = await GetNpzTicksStreamAsync(cancellationToken);

        var data = np.LoadMatrix_Npz(bytes.Select(it => (byte)it).ToArray());

        var time = data["time_msc.npy"];
        var bid = data["bid.npy"];
        var ask = data["ask.npy"];
        var last = data["last.npy"];
        var volume = data["volume.npy"];
        var flags = data["flags.npy"];

        var tempLastTrade = default(SimpleTrade);

        for (var i = 0; i < time.Length - 1; i++)
        {
            var trade = SimpleTrade.Create(
                time.GetValue(i)!,
                bid.GetValue(i)!,
                ask.GetValue(i)!,
                last.GetValue(i)!,
                volume.GetValue(i)!,
                flags.GetValue(i)!);

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
        var fromDate = _lastTrade?.Time ?? (_currentTime - _currentTime.TimeOfDay);
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
            cancellationToken);

        state.CheckResponseStatus(ResponseType.GetTicks, ticksReply.ResponseStatus);

        return ticksReply.Bytes;
    }

    private bool IsNewTrade(SimpleTrade trade)
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
