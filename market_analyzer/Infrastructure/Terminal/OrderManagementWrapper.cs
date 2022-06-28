using Google.Protobuf.WellKnownTypes;
using Grpc.Net.ClientFactory;
using Grpc.Terminal;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Terminal
{
    public interface IOrderManagementWrapper
    {
        Task<GetPositionsReply> GetPositionsAsync(
            string? symbol = null, string? group = null, long? ticket = null, CancellationToken cancellationToken = default);

        Task<GetOrdersReply> GetOrdersAsync(
            string? symbol = null, string? group = null, long? ticket = null, CancellationToken cancellationToken = default);

        Task<GetHistoryOrdersReply> GetHistoryOrdersAsync(
            string group, DateTime utcFromDate, DateTime utcToDate, CancellationToken cancellationToken = default);

        Task<GetHistoryOrdersReply> GetHistoryOrdersAsync(
            long? ticket, long? position, CancellationToken cancellationToken = default);
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
            string? symbol = null, string? group = null, long? ticket = null, CancellationToken cancellationToken = default)
        {
            var client = _grpcClientFactory.CreateClient<OrderManagement.OrderManagementClient>(ClientName);

            var request = new GetPositionsRequest();

            if (!string.IsNullOrWhiteSpace(symbol))
                request.Symbol = symbol.ToUpper().Trim();
            else if (!string.IsNullOrWhiteSpace(group))
                request.Group = group;
            else if (ticket is not null)
                request.Ticket = ticket;
            else
                throw new InvalidOperationException();

            return await client.GetPositionsAsync(request, cancellationToken: cancellationToken);
        }

        public async Task<GetOrdersReply> GetOrdersAsync(
            string? symbol = null, string? group = null, long? ticket = null, CancellationToken cancellationToken = default)
        {
            var client = _grpcClientFactory.CreateClient<OrderManagement.OrderManagementClient>(ClientName);

            var request = new GetOrdersRequest();

            if (!string.IsNullOrWhiteSpace(symbol))
                request.Symbol = symbol.ToUpper().Trim();
            else if (!string.IsNullOrWhiteSpace(group))
                request.Group = group;
            else if (ticket is not null)
                request.Ticket = ticket;
            else
                throw new InvalidOperationException();

            return await client.GetOrdersAsync(request, cancellationToken: cancellationToken);
        }

        public async Task<GetHistoryOrdersReply> GetHistoryOrdersAsync(
            string group, DateTime utcFromDate, DateTime utcToDate, CancellationToken cancellationToken = default)
        {
            var client = _grpcClientFactory.CreateClient<OrderManagement.OrderManagementClient>(ClientName);

            var request = new GetHistoryOrdersRequest
            {
                Group = new GetHistoryOrdersRequest.Types.Group
                {
                    FromDate = Timestamp.FromDateTime(utcFromDate),
                    ToDate = Timestamp.FromDateTime(utcToDate),
                    Value = group,
                }
            };

            return await client.GetHistoryOrdersAsync(request, cancellationToken: cancellationToken);
        }

        public async Task<GetHistoryOrdersReply> GetHistoryOrdersAsync(
            long? ticket, long? position, CancellationToken cancellationToken = default)
        {
            var client = _grpcClientFactory.CreateClient<OrderManagement.OrderManagementClient>(ClientName);

            var request = new GetHistoryOrdersRequest();

            if (ticket is not null)
                request.Ticket = ticket;
            if (position is not null)
                request.Position = position;
            else
                throw new InvalidOperationException();

            return await client.GetHistoryOrdersAsync(request, cancellationToken: cancellationToken);
        }
    }
}