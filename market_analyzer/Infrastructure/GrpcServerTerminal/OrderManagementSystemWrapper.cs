using Google.Protobuf.WellKnownTypes;
using Grpc.Net.ClientFactory;
using Grpc.Terminal;
using Microsoft.Extensions.Logging;

namespace Infrastructure.GrpcServerTerminal;

public interface IOrderManagementSystemWrapper
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

    Task<CheckOrderReply> CheckOrderAsync(OrderRequest request, CancellationToken cancellationToken = default);
    Task<SendOrderReply> SendOrderAsync(OrderRequest request, CancellationToken cancellationToken = default);
}

public class OrderManagementSystemWrapper : IOrderManagementSystemWrapper
{
    public static readonly string ClientName = nameof(OrderManagementSystemWrapper);

    private readonly GrpcClientFactory _grpcClientFactory;
    private readonly ILogger<OrderManagementSystemWrapper> _logger;

    public OrderManagementSystemWrapper(GrpcClientFactory grpcClientFactory, ILogger<OrderManagementSystemWrapper> logger)
    {
        _grpcClientFactory = grpcClientFactory;
        _logger = logger;
    }

    public async Task<GetPositionsReply> GetPositionsAsync(
        string? symbol = null, string? group = null, long? ticket = null,
        CancellationToken cancellationToken = default)
    {
        var client = _grpcClientFactory.CreateClient<OrderManagementSystem.OrderManagementSystemClient>(ClientName);

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
        var client = _grpcClientFactory.CreateClient<OrderManagementSystem.OrderManagementSystemClient>(ClientName);

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
        var client = _grpcClientFactory.CreateClient<OrderManagementSystem.OrderManagementSystemClient>(ClientName);

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
        var client = _grpcClientFactory.CreateClient<OrderManagementSystem.OrderManagementSystemClient>(ClientName);

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
        var client = _grpcClientFactory.CreateClient<OrderManagementSystem.OrderManagementSystemClient>(ClientName);

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
        var client = _grpcClientFactory.CreateClient<OrderManagementSystem.OrderManagementSystemClient>(ClientName);

        var request = new GetHistoryDealsRequest();

        if (ticket is not null)
            request.Ticket = ticket;
        else if (position is not null)
            request.Position = position;
        else
            throw new InvalidOperationException();

        return await client.GetHistoryDealsAsync(request, cancellationToken: cancellationToken);
    }

    public async Task<CheckOrderReply> CheckOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
    {
        var client = _grpcClientFactory.CreateClient<OrderManagementSystem.OrderManagementSystemClient>(ClientName);
        return await client.CheckOrderAsync(request, cancellationToken: cancellationToken);
    }

    public async Task<SendOrderReply> SendOrderAsync(OrderRequest request, CancellationToken cancellationToken = default)
    {
        var client = _grpcClientFactory.CreateClient<OrderManagementSystem.OrderManagementSystemClient>(ClientName);
        return await client.SendOrderAsync(request, cancellationToken: cancellationToken);
    }
}