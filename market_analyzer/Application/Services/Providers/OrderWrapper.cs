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
    public async Task<long?> SellLimitAsync(double price, double volume, long? sellLimitTicket, CancellationToken cancellationToken)
    {
        var order = Order(TradeAction.Pending, OrderType.SellLimit, price, volume, sellLimitTicket);

        var sendOrderReply = await SendOrderAsync(order, cancellationToken);

        return sendOrderReply.Order;
    }

    public async Task<long?> BuyLimitAsync(double price, double volume, long? buyLimitTicket, CancellationToken cancellationToken)
    {
        var order = Order(TradeAction.Pending, OrderType.BuyLimit, price, volume, buyLimitTicket);

        var sendOrderReply = await SendOrderAsync(order, cancellationToken);

        return sendOrderReply.Order;
    }

    public async Task<long?> CancelAsync(long orderTicket, CancellationToken cancellationToken)
    {
        var order = CancelOrder(orderTicket);

        var sendOrderReply = await SendOrderAsync(order, cancellationToken);

        return sendOrderReply.Order;
    }

    private async Task<SendOrderReply> SendOrderAsync(OrderRequest order, CancellationToken cancellationToken)
    {
        var sendOrderReply = await orderManagement.SendOrderAsync(order, cancellationToken);

        state.CheckResponseStatus(ResponseType.SendOrder, sendOrderReply.ResponseStatus);

        if (sendOrderReply.Retcode != TradeRetcode.Done)
        {
            logger.LogError("Error sending order: {error}", sendOrderReply);
        }

        return sendOrderReply;
    }

    private OrderRequest Order(TradeAction tradeAction, OrderType orderType, double price, double lot, long? modifyOrderTicket = null, long? positionTicket = null)
    {
        if (modifyOrderTicket is not null && tradeAction != TradeAction.Pending)
        {
            throw new InvalidOperationException("It's only possible to modify pending order.");
        }

        var settings = options.CurrentValue;

        return new()
        {
            Symbol = settings.Symbol,
            Deviation = settings.Order.Deviation,
            Magic = settings.Order.Magic,
            Price = price,
            Volume = lot,
            Order = modifyOrderTicket,
            Action = modifyOrderTicket is null ? tradeAction : TradeAction.Modify,
            Type = orderType,
            TypeFilling = OrderFilling.Return,
            TypeTime = OrderTime.Day,
            Position = positionTicket,
        };
    }

    private static OrderRequest CancelOrder(long orderTicket)
    {
        return new()
        {
            Order = orderTicket,
            Action = TradeAction.Remove,
        };
    }
}