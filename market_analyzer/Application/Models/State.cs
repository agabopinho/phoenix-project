using Application.Services.Providers.Date;
using Application.Services.Providers.RangeCalculation;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Application.Models;

public class State(IDate dateProvider, ILogger<State> logger)
{
    private readonly ConcurrentBag<ErrorOccurrence> _lastErrors = [];

    private IReadOnlyCollection<Brick> _bricks = [];
    private Position? _position;
    private IReadOnlyCollection<Order> _orders = [];
    private Tick? _lasTick;

    public IReadOnlyCollection<Brick> Bricks => _bricks;
    public DateTime? BricksUpdated { get; private set; }
    public DateTime? LastTradeTime { get; private set; }
    public Position? Position => _position;
    public DateTime? PositionUpdated { get; private set; }
    public IReadOnlyCollection<Order> Orders => _orders;
    public DateTime? OrdersUpdated { get; private set; }
    public Tick? LastTick => _lasTick;
    public DateTime? LastTradeUpdated { get; private set; }
    public IReadOnlyCollection<ErrorOccurrence> LastErrors => _lastErrors;
    public bool WarnAuction => LastTick?.Bid > LastTick?.Ask;

    public void SetBricks(IReadOnlyCollection<Brick> bricks)
    {
        Interlocked.Exchange(ref _bricks, bricks);
        BricksUpdated = DateTime.Now;
    }

    public void SetLastTradeTime(DateTime? lastTradeTime)
    {
        LastTradeTime = lastTradeTime;
    }

    public void SetPosition(Position? position)
    {
        Interlocked.Exchange(ref _position, position);
        PositionUpdated = DateTime.Now;
    }

    public void SetOrders(IReadOnlyCollection<Order> orders)
    {
        Interlocked.Exchange(ref _orders, orders);
        OrdersUpdated = DateTime.Now;
    }

    public void SetLastTick(Tick trade)
    {
        Interlocked.Exchange(ref _lasTick, trade);
        LastTradeUpdated = DateTime.Now;
    }

    public void CheckResponseStatus(ResponseType type, ResponseStatus responseStatus)
    {
        if (responseStatus.ResponseCode == Res.SOk)
        {
            return;
        }

        _lastErrors.Add(new(dateProvider.LocalDateSpecifiedUtcKind(), type, responseStatus));

        logger.LogError("Grpc server error {@data}", new
        {
            ResponseType = type,
            responseStatus.ResponseCode,
            responseStatus.ResponseMessage
        });
    }
}
