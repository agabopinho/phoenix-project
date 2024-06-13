using Application.Models;
using Application.Options;
using Application.Range;
using Application.Services.Providers.Date;
using Grpc.Core;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NumSharp;
using System.Diagnostics;

namespace Application.Services;

public class LoopService(
    IMarketDataWrapper marketDataWrapper,
    IOrderManagementSystemWrapper orderManagementSystemWrapper,
    IDateProvider dateProvider,
    IOptionsMonitor<OperationSettings> operationSettings,
    ILogger<ILoopService> logger) : ILoopService
{
    private const int AHEAD_SECONDS = 30;

    private readonly RangeCalculation _rangeCalculation = new(operationSettings.CurrentValue.BrickSize!.Value);
    private readonly List<ErrorOccurrence> _errors = [];

    private DateTime _currentTime;
    private SimpleTrade? _lastTrade;
    private int _previousBricksCount;
    private int _newBricks;

    private void PreExecution()
    {
        _currentTime = dateProvider.LocalDateSpecifiedUtcKind();
        _errors.Clear();
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        PreExecution();

        var stopwatch = new Stopwatch();

        stopwatch.Start();

        var checkNewPrice = CheckNewPrice(cancellationToken);
        var position = GetPositionAsync(cancellationToken);
        var orders = GetOrdersAsync(cancellationToken);

        await Task.WhenAll(checkNewPrice, position, orders);

        if (_newBricks > 0)
        {
            logger.LogInformation("NewBricks: {newBricks}", _newBricks);
        }

        stopwatch.Stop();
      
        logger.LogInformation("ElapsedMilliseconds: {ElapsedMilliseconds}", stopwatch.ElapsedMilliseconds);
    }

    private async Task CheckNewPrice(CancellationToken cancellationToken)
    {
        _previousBricksCount = _rangeCalculation.Bricks.Count;

        using var stream = await GetNpzTicksStreamAsync(cancellationToken);

        var data = np.LoadMatrix_Npz(stream);

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

    private async Task<Stream> GetNpzTicksStreamAsync(CancellationToken cancellationToken)
    {
        var fromDate = _lastTrade?.Time ?? (_currentTime - _currentTime.TimeOfDay);
        var toDate = _currentTime.AddSeconds(AHEAD_SECONDS);

        if (_previousBricksCount == 0)
        {
            logger.LogInformation("Loading data from: {fromDate}", fromDate);
        }

        using var ticksReply = marketDataWrapper.StreamTicksRangeBytes(
            operationSettings.CurrentValue.Symbol!,
            fromDate,
            toDate,
            CopyTicks.Trade,
            operationSettings.CurrentValue.StreamingData.ChunkSize,
            cancellationToken);

        var stream = new MemoryStream();

        await foreach (var ticksRangeReply in ticksReply.ResponseStream.ReadAllAsync(cancellationToken))
        {
            CheckResponseStatus(ResponseType.GetTicks, ticksRangeReply.ResponseStatus);

            var bytes = ticksRangeReply.Bytes.Select(it => (byte)it).ToArray();

            await stream.WriteAsync(bytes, cancellationToken);
        }

        stream.Position = 0;

        return stream;
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

    private async Task<IEnumerable<Order>> GetOrdersAsync(CancellationToken cancellationToken)
    {
        var orders = await orderManagementSystemWrapper.GetOrdersAsync(
            symbol: operationSettings.CurrentValue.Symbol,
            group: null,
            ticket: null,
            cancellationToken: cancellationToken);

        CheckResponseStatus(ResponseType.GetOrder, orders.ResponseStatus);

        return orders.Orders ?? [];
    }

    private async Task<Position?> GetPositionAsync(CancellationToken cancellationToken)
    {
        var positions = await orderManagementSystemWrapper.GetPositionsAsync(
            symbol: operationSettings.CurrentValue.Symbol!,
            group: null,
            ticket: null,
            cancellationToken: cancellationToken);

        CheckResponseStatus(ResponseType.GetPosition, positions.ResponseStatus);

        return positions.Positions?.FirstOrDefault();
    }

    private void CheckResponseStatus(ResponseType type, ResponseStatus responseStatus)
    {
        if (responseStatus.ResponseCode == Res.SOk)
        {
            return;
        }

        _errors.Add(new(dateProvider.LocalDateSpecifiedUtcKind(), type, responseStatus));

        logger.LogError("Grpc server error {@data}", new
        {
            responseStatus.ResponseCode,
            responseStatus.ResponseMessage
        });
    }
}
