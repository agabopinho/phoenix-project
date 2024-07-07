using Application.Models;
using Application.Options;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services.Providers;

public interface IOrderWrapper
{
    Task<long?> BuyAsync(double price, double volume, CancellationToken cancellationToken);
    Task<long?> BuyAsync(double price, double volume, long magic, CancellationToken cancellationToken);
    Task<long?> BuyLimitAsync(double price, double volume, CancellationToken cancellationToken);
    Task<long?> BuyLimitAsync(double price, double volume, long magic, CancellationToken cancellationToken);
    Task<long?> CancelAsync(long orderTicket, CancellationToken cancellationToken);
    Task<long?> ModifyOrderLimitAsync(long orderTicket, double price, CancellationToken cancellationToken);
    Task<long?> SellAsync(double price, double volume, CancellationToken cancellationToken);
    Task<long?> SellAsync(double price, double volume, long magic, CancellationToken cancellationToken);
    Task<long?> SellLimitAsync(double price, double volume, CancellationToken cancellationToken);
    Task<long?> SellLimitAsync(double price, double volume, long magic, CancellationToken cancellationToken);
    Task<SendOrderReply?> SendOrderAsync(OrderRequest order, CancellationToken cancellationToken);
}

public class OrderWrapper(IOrderManagementSystemWrapper orderManagement, State state, IOptionsMonitor<OperationOptions> options, ILogger<OrderWrapper> logger)
    : IOrderWrapper
{
    public Task<long?> SellLimitAsync(double price, double volume, CancellationToken cancellationToken)
    {
        return SellLimitAsync(price, volume, options.CurrentValue.Order.Magic, cancellationToken);
    }

    public async Task<long?> SellLimitAsync(double price, double volume, long magic, CancellationToken cancellationToken)
    {
        var order = OrderLimit(options.CurrentValue, OrderType.SellLimit, price, volume, magic);

        var sendOrderReply = await SendOrderAsync(order, cancellationToken);

        return sendOrderReply?.Order;
    }

    public Task<long?> BuyLimitAsync(double price, double volume, CancellationToken cancellationToken)
    {
        return BuyLimitAsync(price, volume, options.CurrentValue.Order.Magic, cancellationToken);
    }

    public async Task<long?> BuyLimitAsync(double price, double volume, long magic, CancellationToken cancellationToken)
    {
        var order = OrderLimit(options.CurrentValue, OrderType.BuyLimit, price, volume, magic);

        var sendOrderReply = await SendOrderAsync(order, cancellationToken);

        return sendOrderReply?.Order;
    }

    public async Task<long?> ModifyOrderLimitAsync(long orderTicket, double price, CancellationToken cancellationToken)
    {
        var order = ModifyOrderLimit(options.CurrentValue, orderTicket, price);

        var sendOrderReply = await SendOrderAsync(order, cancellationToken);

        return sendOrderReply?.Order;
    }

    public async Task<long?> CancelAsync(long orderTicket, CancellationToken cancellationToken)
    {
        var order = CancelOrderLimit(options.CurrentValue, orderTicket);

        var sendOrderReply = await SendOrderAsync(order, cancellationToken);

        return sendOrderReply?.Order;
    }

    public Task<long?> BuyAsync(double price, double volume, CancellationToken cancellationToken)
    {
        return BuyAsync(price, volume, options.CurrentValue.Order.Magic, cancellationToken);
    }

    public async Task<long?> BuyAsync(double price, double volume, long magic, CancellationToken cancellationToken)
    {
        var order = MarketOrder(options.CurrentValue, OrderType.Buy, price, volume, magic);

        var sendOrderReply = await SendOrderAsync(order, cancellationToken);

        return sendOrderReply?.Order;
    }

    public Task<long?> SellAsync(double price, double volume, CancellationToken cancellationToken)
    {
        return SellAsync(price, volume, options.CurrentValue.Order.Magic, cancellationToken);
    }

    public async Task<long?> SellAsync(double price, double volume, long magic, CancellationToken cancellationToken)
    {
        var order = MarketOrder(options.CurrentValue, OrderType.Sell, price, volume, magic);

        var sendOrderReply = await SendOrderAsync(order, cancellationToken);

        return sendOrderReply?.Order;
    }

    public async Task<SendOrderReply?> SendOrderAsync(OrderRequest order, CancellationToken cancellationToken)
    {
        var sendOrderReply = await orderManagement.SendOrderAsync(order, cancellationToken);

        state.CheckResponseStatus(ResponseType.SendOrder, sendOrderReply.ResponseStatus, sendOrderReply.Comment);

        if (sendOrderReply.Retcode != TradeRetcode.Done)
        {
            logger.LogError("Error sending order: {@error}", sendOrderReply);
        }

        return sendOrderReply;
    }

    private static OrderRequest MarketOrder(OperationOptions settings, OrderType orderType, double price, double lot, long magic)
    {
        if (orderType is not (OrderType.Buy or OrderType.Sell))
        {
            throw new InvalidOperationException("Invalid order type.");
        }

        return new()
        {
            Symbol = settings.Symbol,
            Deviation = settings.Order.Deviation,
            Magic = magic,
            Price = price,
            Volume = lot,
            Action = TradeAction.Deal,
            Type = orderType,
            TypeFilling = OrderFilling.Return,
            TypeTime = OrderTime.Day,
        };
    }

    private static OrderRequest OrderLimit(OperationOptions settings, OrderType orderType, double price, double lot, long magic)
    {
        if (orderType is not (OrderType.BuyLimit or OrderType.SellLimit))
        {
            throw new InvalidOperationException("Invalid order type.");
        }

        return new()
        {
            Symbol = settings.Symbol,
            Deviation = settings.Order.Deviation,
            Magic = magic,
            Price = price,
            Volume = lot,
            Action = TradeAction.Pending,
            Type = orderType,
            TypeFilling = OrderFilling.Return,
            TypeTime = OrderTime.Day,
        };
    }

    private static OrderRequest CancelOrderLimit(OperationOptions _, long orderTicket)
    {
        return new()
        {
            Order = orderTicket,
            Action = TradeAction.Remove,
        };
    }

    private static OrderRequest ModifyOrderLimit(OperationOptions _, long orderTicket, double price)
    {
        return new()
        {
            Order = orderTicket,
            Price = price,
            Action = TradeAction.Modify,
            TypeTime = OrderTime.Day,
        };
    }
}