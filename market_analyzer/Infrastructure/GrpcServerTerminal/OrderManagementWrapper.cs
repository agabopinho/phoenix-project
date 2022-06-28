using Google.Protobuf.WellKnownTypes;
using Grpc.Net.ClientFactory;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Microsoft.Extensions.Logging;

namespace Infrastructure.GrpcServerTerminal
{
    public interface IOrderManagementWrapper
    {
        Task<GetPositionsReply> GetPositionsAsync(
            string? symbol = null, string? group = null, long? ticket = null,
            CancellationToken cancellationToken = default);

        Task<GetOrdersReply> GetOrdersAsync(
            string? symbol = null, string? group = null, long? ticket = null,
            CancellationToken cancellationToken = default);

        Task<GetHistoryOrdersReply> GetHistoryOrdersAsync(
            string group, DateTime utcFromDate, DateTime utcToDate,
            CancellationToken cancellationToken = default);

        Task<GetHistoryOrdersReply> GetHistoryOrdersAsync(
            long? ticket = null, long? position = null,
            CancellationToken cancellationToken = default);

        Task<GetHistoryDealsReply> GetHistoryDealsAsync(
            string group, DateTime utcFromDate, DateTime utcToDate,
            CancellationToken cancellationToken = default);

        Task<GetHistoryDealsReply> GetHistoryDealsAsync(
            long? ticket = null, long? position = null,
            CancellationToken cancellationToken = default);
    }

    public class OrderManagementWrapper : IOrderManagementWrapper
    {
        public static readonly string ClientName = nameof(OrderManagementWrapper);

        private readonly GrpcClientFactory _grpcClientFactory;
        private readonly ILogger<OrderManagementWrapper> _logger;

        public OrderManagementWrapper(GrpcClientFactory grpcClientFactory, ILogger<OrderManagementWrapper> logger)
        {
            _grpcClientFactory = grpcClientFactory;
            _logger = logger;
        }

        public async Task<GetPositionsReply> GetPositionsAsync(
            string? symbol = null, string? group = null, long? ticket = null,
            CancellationToken cancellationToken = default)
        {
            var client = _grpcClientFactory.CreateClient<OrderManagement.OrderManagementClient>(ClientName);

            var request = new GetPositionsRequest();

            if (!string.IsNullOrWhiteSpace(symbol))
                request.Symbol = symbol.ToUpper().Trim();
            else if (!string.IsNullOrWhiteSpace(group))
                request.Group = group;
            else if (ticket is not null)
                request.Ticket = ticket;

            return await client.GetPositionsAsync(request, cancellationToken: cancellationToken);
        }

        public async Task<GetOrdersReply> GetOrdersAsync(
            string? symbol = null, string? group = null, long? ticket = null,
            CancellationToken cancellationToken = default)
        {
            var client = _grpcClientFactory.CreateClient<OrderManagement.OrderManagementClient>(ClientName);

            var request = new GetOrdersRequest();

            if (!string.IsNullOrWhiteSpace(symbol))
                request.Symbol = symbol.ToUpper().Trim();
            else if (!string.IsNullOrWhiteSpace(group))
                request.Group = group;
            else if (ticket is not null)
                request.Ticket = ticket;

            return await client.GetOrdersAsync(request, cancellationToken: cancellationToken);
        }

        public async Task<GetHistoryOrdersReply> GetHistoryOrdersAsync(
            string group, DateTime utcFromDate, DateTime utcToDate,
            CancellationToken cancellationToken = default)
        {
            var client = _grpcClientFactory.CreateClient<OrderManagement.OrderManagementClient>(ClientName);

            var request = new GetHistoryOrdersRequest
            {
                Group = new GetHistoryOrdersRequest.Types.Group
                {
                    FromDate = Timestamp.FromDateTime(utcFromDate),
                    ToDate = Timestamp.FromDateTime(utcToDate),
                    GroupValue = group,
                }
            };

            return await client.GetHistoryOrdersAsync(request, cancellationToken: cancellationToken);
        }

        public async Task<GetHistoryOrdersReply> GetHistoryOrdersAsync(
            long? ticket = null, long? position = null,
            CancellationToken cancellationToken = default)
        {
            var client = _grpcClientFactory.CreateClient<OrderManagement.OrderManagementClient>(ClientName);

            var request = new GetHistoryOrdersRequest();

            if (ticket is not null)
                request.Ticket = ticket;
            else if (position is not null)
                request.Position = position;
            else
                throw new InvalidOperationException();

            return await client.GetHistoryOrdersAsync(request, cancellationToken: cancellationToken);
        }

        public async Task<GetHistoryDealsReply> GetHistoryDealsAsync(
            string group, DateTime utcFromDate, DateTime utcToDate,
            CancellationToken cancellationToken = default)
        {
            var client = _grpcClientFactory.CreateClient<OrderManagement.OrderManagementClient>(ClientName);

            var request = new GetHistoryDealsRequest
            {
                Group = new GetHistoryDealsRequest.Types.Group
                {
                    FromDate = Timestamp.FromDateTime(utcFromDate),
                    ToDate = Timestamp.FromDateTime(utcToDate),
                    GroupValue = group,
                }
            };

            return await client.GetHistoryDealsAsync(request, cancellationToken: cancellationToken);
        }

        public async Task<GetHistoryDealsReply> GetHistoryDealsAsync(
            long? ticket = null, long? position = null,
            CancellationToken cancellationToken = default)
        {
            var client = _grpcClientFactory.CreateClient<OrderManagement.OrderManagementClient>(ClientName);

            var request = new GetHistoryDealsRequest();

            if (ticket is not null)
                request.Ticket = ticket;
            else if (position is not null)
                request.Position = position;
            else
                throw new InvalidOperationException();

            return await client.GetHistoryDealsAsync(request, cancellationToken: cancellationToken);
        }

        public async Task<CheckOrderReply> CheckOrderAsync(OrderRequest orderRequest, CancellationToken cancellationToken = default)
        {
            var client = _grpcClientFactory.CreateClient<OrderManagement.OrderManagementClient>(ClientName);
            return await client.CheckOrderAsync(orderRequest, cancellationToken: cancellationToken);
        }

        public async Task<SendOrderReply> SendOrderAsync(OrderRequest orderRequest, CancellationToken cancellationToken = default)
        {
            var client = _grpcClientFactory.CreateClient<OrderManagement.OrderManagementClient>(ClientName);
            return await client.SendOrderAsync(orderRequest, cancellationToken: cancellationToken);
        }
    }

    public interface IOrderCreator
    {
    }

    public class OrderCreator : IOrderCreator
    {
        /// <summary>
        /// Place a trade order for an immediate execution with the specified parameters (market order)
        /// </summary>
        /// <returns></returns>
        public OrderRequest MarketOrder()
            => new()
            {
                Action = TradeAction.Deal
            };

        /// <summary>
        /// Place a trade order for the execution under specified conditions (pending order)
        /// </summary>
        /// <returns></returns>
        public OrderRequest PendingOrder()
           => new()
           {
               Action = TradeAction.Pending
           };

        /// <summary>
        /// Modify Stop Loss and Take Profit values of an opened position
        /// </summary>
        /// <returns></returns>
        public OrderRequest ModifyOpenOrder()
            => new()
            {
                Action = TradeAction.Sltp
            };

        /// <summary>
        /// Modify the parameters of the order placed previously
        /// </summary>
        /// <returns></returns>
        public OrderRequest ModifyPlacedOrder()
            => new()
            {
                Action = TradeAction.Modify
            };

        /// <summary>
        /// Delete the pending order placed previously
        /// </summary>
        /// <returns></returns>
        public OrderRequest RemovePlacedOrder()
            => new()
            {
                Action = TradeAction.Remove
            };

        /// <summary>
        /// Close a position by an opposite one
        /// </summary>
        /// <returns></returns>
        public OrderRequest ClosePositionByAnOppositeOne()
            => new()
            {
                Action = TradeAction.CloseBy
            };
    }
}