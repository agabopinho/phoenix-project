using Application.Models;
using Application.Options;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Services.Providers;

public class OrderWrapper(IOrderManagementSystemWrapper orderManagement, State state, IOptionsMonitor<OperationOptions> options, ILogger<OrderWrapper> logger)
{
    public async Task<long?> SellLimitAsync(double price, double volume, long? magic, CancellationToken cancellationToken)
    {
        var order = OrderLimit(options.CurrentValue, OrderType.SellLimit, price, volume, magic);

        var sendOrderReply = await SendOrderAsync(order, cancellationToken);

        return sendOrderReply?.Order;
    }

    public async Task<long?> BuyLimitAsync(double price, double volume, long? magic, CancellationToken cancellationToken)
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

    private static OrderRequest OrderLimit(OperationOptions settings, OrderType orderType, double price, double lot, long? magic = null)
    {
        if (orderType is not (OrderType.BuyLimit or OrderType.SellLimit))
        {
            throw new InvalidOperationException("Invalid order type.");
        }

        return new()
        {
            Symbol = settings.Symbol,
            Deviation = settings.Order.Deviation,
            Magic = magic ?? settings.Order.Magic,
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