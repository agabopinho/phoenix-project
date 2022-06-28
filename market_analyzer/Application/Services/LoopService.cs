using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Application.Services
{
    public interface ILoopService
    {
        Task RunAsync(CancellationToken cancellationToken);
    }

    public static class Operation
    {
        public static readonly string Symbol = "GBPUSD";
        public static readonly DateOnly Date = new(2022, 6, 27);
        public static readonly int ChunkSize = 5000;
        public static readonly TimeSpan Timeframe = TimeSpan.FromSeconds(5);
    }

    public class LoopService : ILoopService
    {
        private readonly IRatesStateService _ratesStateService;
        private readonly IOrderManagementWrapper _orderManagementWrapper;
        private readonly ILogger<ILoopService> _logger;

        public LoopService(IRatesStateService ratesStateService, IOrderManagementWrapper orderManagementWrapper, ILogger<ILoopService> logger)
        {
            _ratesStateService = ratesStateService;
            _orderManagementWrapper = orderManagementWrapper;
            _logger = logger;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            
            var check = CheckAsync(cancellationToken);
            
            var histOrders = _orderManagementWrapper.GetHistoryOrdersAsync("*", DateTime.UtcNow.AddYears(-5), DateTime.UtcNow.AddDays(1), cancellationToken);
            var histOrderById = _orderManagementWrapper.GetHistoryOrdersAsync(ticket: 1380587995, cancellationToken: cancellationToken);
            
            var orders = _orderManagementWrapper.GetOrdersAsync(group: "*", cancellationToken: cancellationToken);
            var orderById = _orderManagementWrapper.GetOrdersAsync(ticket: 1380592911, cancellationToken: cancellationToken);
            
            var positions = _orderManagementWrapper.GetPositionsAsync(group: "*", cancellationToken: cancellationToken);
            var positionById = _orderManagementWrapper.GetPositionsAsync(ticket: 1380592864, cancellationToken: cancellationToken);
             
            var deals = _orderManagementWrapper.GetHistoryDealsAsync("*", DateTime.UtcNow.AddYears(-5), DateTime.UtcNow.AddDays(1), cancellationToken);
            var dealById = _orderManagementWrapper.GetHistoryDealsAsync(ticket: 1360478968, cancellationToken: cancellationToken);

            await Task.WhenAll(check, histOrders, orders, positions, deals, histOrderById, orderById, positionById, dealById);
            
            stopwatch.Stop();
            
            _logger.LogInformation("Run in {@data}ms", stopwatch.Elapsed.TotalMilliseconds);
        }

        private async Task CheckAsync(CancellationToken cancellationToken)
        {
            await _ratesStateService.CheckNewRatesAsync(
                Operation.Symbol, Operation.Date, Operation.Timeframe,
                Operation.ChunkSize, cancellationToken);

            var r = await _ratesStateService.GetRatesAsync(
                Operation.Symbol, Operation.Date, Operation.Timeframe,
                TimeSpan.FromMinutes(30), cancellationToken);
        }
    }
}
