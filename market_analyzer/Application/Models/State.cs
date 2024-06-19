using Application.Helpers;
using Application.Options;
using Application.Services.Providers.Date;
using Application.Services.Providers.Range;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Application.Models;

public class State(IDate dateProvider, ILogger<State> logger, IOptionsMonitor<OperationOptions> operationSettings)
{
    private readonly ConcurrentBag<ErrorOccurrence> _lastErrors = [];

    private IReadOnlyCollection<Brick> _bricks = [];
    private Trade? _lastBricksTrade;
    private Position? _position;
    private IReadOnlyCollection<Order> _orders = [];
    private Tick? _lastTick;

    private double _bricksUpdated;
    private double _positionUpdated;
    private double _ordersUpdated;
    private double _lastTickUpdated;
    private int _sanityTestStatus;

    public IReadOnlyCollection<Brick> Bricks => _bricks;
    public Trade? LastBricksTrade => _lastBricksTrade;
    public Position? Position => _position;
    public IReadOnlyCollection<Order> Orders => _orders;
    public Tick? LastTick => _lastTick;
    public IReadOnlyCollection<ErrorOccurrence> LastErrors => _lastErrors;

    public DateTime BricksUpdated => _bricksUpdated.DateTimeFromUnixEpochMilliseconds();
    public DateTime PositionUpdated => _positionUpdated.DateTimeFromUnixEpochMilliseconds();
    public DateTime OrdersUpdated => _ordersUpdated.DateTimeFromUnixEpochMilliseconds();
    public DateTime LastTickUpdated => _lastTickUpdated.DateTimeFromUnixEpochMilliseconds();

    public bool WarnAuction => LastTick?.Bid > LastTick?.Ask;
    public bool OpenMarket => LastTick is not null && LastTick.Time.ToDateTime() > DateTime.Today;
    public bool ReadyForTrading => ReadyForSanityTest && SanityTestStatus is SanityTestStatus.Skipped or SanityTestStatus.Passed;
    public bool ReadyForSanityTest => OpenMarket && !WarnAuction;
    public SanityTestStatus SanityTestStatus => (SanityTestStatus)_sanityTestStatus;

    public bool Delayed
    {
        get
        {
            var updates = new[] { PositionUpdated, OrdersUpdated, LastTickUpdated };
            return CheckDelayed(updates.Min());
        }
    }

    public bool CheckDelayed(DateTime updatedAt)
    {
        var delay = DateTime.UtcNow - updatedAt;
        return delay.TotalMilliseconds > operationSettings.CurrentValue.MaximumInformationDelay;
    }

    public void SetBricks(IReadOnlyCollection<Brick> bricks, Trade? lastTrade)
    {
        Interlocked.Exchange(ref _bricks, bricks);
        Interlocked.Exchange(ref _lastBricksTrade, lastTrade);
        Interlocked.Exchange(ref _bricksUpdated, DateTime.UtcNow.ToUnixEpochMilliseconds());
    }

    public void SetPosition(Position? position)
    {
        Interlocked.Exchange(ref _position, position);
        Interlocked.Exchange(ref _positionUpdated, DateTime.UtcNow.ToUnixEpochMilliseconds());
    }

    public void SetOrders(IReadOnlyCollection<Order> orders)
    {
        Interlocked.Exchange(ref _orders, orders);
        Interlocked.Exchange(ref _ordersUpdated, DateTime.UtcNow.ToUnixEpochMilliseconds());
    }

    public void SetLastTick(Tick trade)
    {
        Interlocked.Exchange(ref _lastTick, trade);
        Interlocked.Exchange(ref _lastTickUpdated, DateTime.UtcNow.ToUnixEpochMilliseconds());
    }

    public void SetSanityTestStatus(SanityTestStatus sanityTestStatus)
    {
        Interlocked.Exchange(ref _sanityTestStatus, (int)sanityTestStatus);
    }

    public void CheckResponseStatus(ResponseType type, ResponseStatus responseStatus, string? comment = null)
    {
        if (responseStatus.ResponseCode == Res.SOk)
        {
            return;
        }

        _lastErrors.Add(new(dateProvider.LocalDateSpecifiedUtcKind(), type, responseStatus, comment));

        logger.LogError("Grpc server error {@data}", new
        {
            ResponseType = type,
            responseStatus.ResponseCode,
            responseStatus.ResponseMessage
        });
    }
}