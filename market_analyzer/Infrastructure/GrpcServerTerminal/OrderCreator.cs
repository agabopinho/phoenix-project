using Grpc.Terminal;
using Grpc.Terminal.Enums;

namespace Infrastructure.GrpcServerTerminal
{
    public interface IOrderCreator
    {
        /// <summary>
        /// Place a trade order for an immediate execution with the specified parameters (market order)
        /// </summary>
        /// <returns></returns>
        OrderRequest BuyAtMarket(
            string symbol, double price, double volume,
            int deviation, double? stopLoss = null, double? takeProfit = null,
            long? position = null, long? magic = null, string? comment = null,
            OrderFilling typeFilling = OrderFilling.Return);

        /// <summary>
        /// Place a trade order for an immediate execution with the specified parameters (market order)
        /// </summary>
        /// <returns></returns>
        OrderRequest SellAtMarket(
            string symbol, double price, double volume,
            int deviation, double? stopLoss = null, double? takeProfit = null,
            long? position = null, long? magic = null, string? comment = null,
            OrderFilling typeFilling = OrderFilling.Return);
    }

    public class OrderCreator : IOrderCreator
    {
        /// <summary>
        /// Place a trade order for an immediate execution with the specified parameters (market order)
        /// </summary>
        /// <returns></returns>
        public OrderRequest BuyAtMarket(
            string symbol,
            double price,
            double volume,
            int deviation,
            double? stopLoss,
            double? takeProfit,
            long? position,
            long? magic = null,
            string? comment = null,
            OrderFilling typeFilling = OrderFilling.Return)
            => new()
            {
                Action = TradeAction.Deal,
                Symbol = symbol,
                Comment = comment,
                Deviation = deviation,
                Magic = magic,
                Price = price,
                StopLoss = stopLoss,
                TakeProfit = takeProfit,
                Type = OrderType.Buy,
                TypeFilling = typeFilling,
                TypeTime = OrderTime.Gtc,
                Volume = volume
            };

        /// <summary>
        /// Place a trade order for an immediate execution with the specified parameters (market order)
        /// </summary>
        /// <returns></returns>
        public OrderRequest SellAtMarket(
            string symbol,
            double price,
            double volume,
            int deviation,
            double? stopLoss,
            double? takeProfit,
            long? position,
            long? magic = null,
            string? comment = null,
            OrderFilling typeFilling = OrderFilling.Return)
            => new()
            {
                Action = TradeAction.Deal,
                Symbol = symbol,
                Comment = comment,
                Deviation = deviation,
                Magic = magic,
                Price = price,
                StopLoss = stopLoss,
                TakeProfit = takeProfit,
                Type = OrderType.Sell,
                TypeFilling = typeFilling,
                TypeTime = OrderTime.Gtc,
                Volume = volume
            };
    }
}