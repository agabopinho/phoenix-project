using Google.Protobuf.WellKnownTypes;
using Grpc.Net.ClientFactory;
using Grpc.Terminal;

namespace Infrastructure.GrpcServerTerminal;

public interface IOrderManagementSystemWrapper
{
    Task<GetPositionsReply> GetPositionsAsync(
        string? symbol,
        string? group,
        long? ticket,
        CancellationToken cancellationToken);

    Task<GetOrdersReply> GetOrdersAsync(
        string? symbol,
        string? group,
        long? ticket,
        CancellationToken cancellationToken);

    Task<GetHistoryOrdersReply> GetHistoryOrdersAsync(
        string group,
        DateTime utcFromDate,
        DateTime utcToDate,
        CancellationToken cancellationToken);

    Task<GetHistoryOrdersReply> GetHistoryOrdersAsync(
        long? ticket,
        long? position,
        CancellationToken cancellationToken);

    Task<GetHistoryDealsReply> GetHistoryDealsAsync(
        string group,
        DateTime utcFromDate,
        DateTime utcToDate,
        CancellationToken cancellationToken);

    Task<GetHistoryDealsReply> GetHistoryDealsAsync(
        long? ticket,
        long? position,
        CancellationToken cancellationToken);

    Task<CheckOrderReply> CheckOrderAsync(OrderRequest request, CancellationToken cancellationToken);

    Task<SendOrderReply> SendOrderAsync(OrderRequest request, CancellationToken cancellationToken);
}

public class OrderManagementSystemWrapper(GrpcClientFactory clientFactory) : IOrderManagementSystemWrapper
{
    public static readonly string ClientName = nameof(OrderManagementSystemWrapper);

    public async Task<GetPositionsReply> GetPositionsAsync(
        string? symbol,
        string? group,
        long? ticket,
        CancellationToken cancellationToken)
    {
        var client = clientFactory.CreateClient<OrderManagementSystem.OrderManagementSystemClient>(ClientName);

        var request = new GetPositionsRequest();

        if (!string.IsNullOrWhiteSpace(symbol))
        {
            request.Symbol = symbol.ToUpper().Trim();
        }
        else if (!string.IsNullOrWhiteSpace(group))
        {
            request.Group = group;
        }
        else if (ticket is not null)
        {
            request.Ticket = ticket;
        }

        return await client.GetPositionsAsync(request, cancellationToken: cancellationToken);
    }

    public async Task<GetOrdersReply> GetOrdersAsync(
        string? symbol,
        string? group,
        long? ticket,
        CancellationToken cancellationToken)
    {
        var client = clientFactory.CreateClient<OrderManagementSystem.OrderManagementSystemClient>(ClientName);

        var request = new GetOrdersRequest();

        if (!string.IsNullOrWhiteSpace(symbol))
        {
            request.Symbol = symbol.ToUpper().Trim();
        }
        else if (!string.IsNullOrWhiteSpace(group))
        {
            request.Group = group;
        }
        else if (ticket is not null)
        {
            request.Ticket = ticket;
        }

        return await client.GetOrdersAsync(request, cancellationToken: cancellationToken);
    }

    public async Task<GetHistoryOrdersReply> GetHistoryOrdersAsync(
        string group,
        DateTime utcFromDate,
        DateTime utcToDate,
        CancellationToken cancellationToken)
    {
        var client = clientFactory.CreateClient<OrderManagementSystem.OrderManagementSystemClient>(ClientName);

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
        long? ticket,
        long? position,
        CancellationToken cancellationToken)
    {
        var client = clientFactory.CreateClient<OrderManagementSystem.OrderManagementSystemClient>(ClientName);

        var request = new GetHistoryOrdersRequest();

        if (ticket is not null)
        {
            request.Ticket = ticket;
        }
        else if (position is not null)
        {
            request.Position = position;
        }
        else
        {
            throw new InvalidOperationException();
        }

        return await client.GetHistoryOrdersAsync(request, cancellationToken: cancellationToken);
    }

    public async Task<GetHistoryDealsReply> GetHistoryDealsAsync(
        string group,
        DateTime utcFromDate,
        DateTime utcToDate,
        CancellationToken cancellationToken)
    {
        var client = clientFactory.CreateClient<OrderManagementSystem.OrderManagementSystemClient>(ClientName);

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
        long? ticket,
        long? position,
        CancellationToken cancellationToken)
    {
        var client = clientFactory.CreateClient<OrderManagementSystem.OrderManagementSystemClient>(ClientName);

        var request = new GetHistoryDealsRequest();

        if (ticket is not null)
        {
            request.Ticket = ticket;
        }
        else if (position is not null)
        {
            request.Position = position;
        }
        else
        {
            throw new InvalidOperationException();
        }

        return await client.GetHistoryDealsAsync(request, cancellationToken: cancellationToken);
    }

    public async Task<CheckOrderReply> CheckOrderAsync(OrderRequest request, CancellationToken cancellationToken)
    {
        var client = clientFactory.CreateClient<OrderManagementSystem.OrderManagementSystemClient>(ClientName);
        return await client.CheckOrderAsync(request, cancellationToken: cancellationToken);
    }

    public async Task<SendOrderReply> SendOrderAsync(OrderRequest request, CancellationToken cancellationToken)
    {
        var client = clientFactory.CreateClient<OrderManagementSystem.OrderManagementSystemClient>(ClientName);
        return await client.SendOrderAsync(request, cancellationToken: cancellationToken);
    }
}