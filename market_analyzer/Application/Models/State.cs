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
    private readonly ConcurrentBag<ErrorOccurrence> _errors = [];
    private readonly ConcurrentDictionary<string, RangeChart> _charts = [];

    private Position? _position;
    private IReadOnlyCollection<Order> _orders = [];
    private Tick? _lastTick;

    private double _chartUpdatedAt;
    private double _positionUpdatedAt;
    private double _ordersUpdatedAt;
    private double _lastTickUpdatedAt;
    private int _sanityTestStatus;

    public ConcurrentDictionary<string, RangeChart> Charts => _charts;
    public Position? Position => _position;
    public IReadOnlyCollection<Order> Orders => _orders;
    public Tick? LastTick => _lastTick;
    public IReadOnlyCollection<ErrorOccurrence> LastErrors => _errors;

    public DateTime BricksUpdatedAt => _chartUpdatedAt.DateTimeFromUnixEpochMilliseconds();
    public DateTime PositionUpdatedAt => _positionUpdatedAt.DateTimeFromUnixEpochMilliseconds();
    public DateTime OrdersUpdatedAt => _ordersUpdatedAt.DateTimeFromUnixEpochMilliseconds();
    public DateTime LastTickUpdatedAt => _lastTickUpdatedAt.DateTimeFromUnixEpochMilliseconds();

    public bool WarnAuction => LastTick?.Bid > LastTick?.Ask;
    public bool OpenMarket => LastTick is not null && LastTick.Time.ToDateTime() > DateTime.Today;
    public bool ReadyForTrading => ReadyForSanityTest && SanityTestStatus is SanityTestStatus.Skipped or SanityTestStatus.Passed;
    public bool ReadyForSanityTest => OpenMarket && !WarnAuction;
    public SanityTestStatus SanityTestStatus => (SanityTestStatus)_sanityTestStatus;

    public bool Delayed
    {
        get
        {
            DateTime[] updates = [PositionUpdatedAt, OrdersUpdatedAt, LastTickUpdatedAt];
            return CheckDelayed(updates.Min());
        }
    }

    public bool CheckDelayed(DateTime updatedAt)
    {
        var delay = DateTime.UtcNow - updatedAt;
        return delay.TotalMilliseconds > operationSettings.CurrentValue.Order.MaximumInformationDelay;
    }

    public void SetCharts(string name, RangeChart chart)
    {
        _charts.AddOrUpdate(name, chart, (_, _) => chart);
        Interlocked.Exchange(ref _chartUpdatedAt, DateTime.UtcNow.ToUnixEpochMilliseconds());
    }

    public void SetPosition(Position? position)
    {
        Interlocked.Exchange(ref _position, position);
        Interlocked.Exchange(ref _positionUpdatedAt, DateTime.UtcNow.ToUnixEpochMilliseconds());
    }

    public void SetOrders(IReadOnlyCollection<Order> orders)
    {
        Interlocked.Exchange(ref _orders, orders);
        Interlocked.Exchange(ref _ordersUpdatedAt, DateTime.UtcNow.ToUnixEpochMilliseconds());
    }

    public void SetLastTick(Tick trade)
    {
        Interlocked.Exchange(ref _lastTick, trade);
        Interlocked.Exchange(ref _lastTickUpdatedAt, DateTime.UtcNow.ToUnixEpochMilliseconds());
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

        _errors.Add(new(dateProvider.LocalDateSpecifiedUtcKind(), type, responseStatus, comment));

        logger.LogError("Grpc server error {@data}", new
        {
            ResponseType = type,
            responseStatus.ResponseCode,
            responseStatus.ResponseMessage
        });
    }
}