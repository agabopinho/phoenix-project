using Application.Helpers;
using Grpc.Core;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
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
        public static readonly string Symbol = "WINQ22";
        public static readonly DateOnly Date = new(2022, 6, 28);
        public static readonly int ChunkSize = 5000;
        public static readonly TimeSpan Timeframe = TimeSpan.FromSeconds(5);
    }

    public class LoopService : ILoopService
    {
        private readonly IRatesStateService _ratesStateService;
        private readonly IMarketDataWrapper _marketDataWrapper;
        private readonly IOrderManagementWrapper _orderManagementWrapper;
        private readonly IOrderCreator _orderCreator;
        private readonly ILogger<ILoopService> _logger;

        public LoopService(
            IRatesStateService ratesStateService,
            IMarketDataWrapper marketDataWrapper,
            IOrderManagementWrapper orderManagementWrapper,
            IOrderCreator orderCreator,
            ILogger<ILoopService> logger)
        {
            _ratesStateService = ratesStateService;
            _marketDataWrapper = marketDataWrapper;
            _orderManagementWrapper = orderManagementWrapper;
            _orderCreator = orderCreator;
            _logger = logger;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var symbol = "USDJPY";
            var now = DateTime.UtcNow.AddHours(3);

            var rates = await GetRatesAsync(symbol, now, cancellationToken);
            var price = rates.OrderByDescending(it => it.Time).First().Close!.Value;

            var request = _orderCreator.BuyAtMarket(symbol, 0, 1, 20, comment: "test order");
            var response = await _orderManagementWrapper.SendOrderAsync(request, cancellationToken);

            var histOrders = _orderManagementWrapper.GetHistoryOrdersAsync(group: symbol, DateTime.UtcNow.AddYears(-5), DateTime.UtcNow.AddDays(1), cancellationToken);
            var orders = _orderManagementWrapper.GetOrdersAsync(group: symbol, cancellationToken: cancellationToken);
            var positions = _orderManagementWrapper.GetPositionsAsync(group: symbol, cancellationToken: cancellationToken);

            await Task.WhenAll(histOrders, orders, positions);

            stopwatch.Stop();

            _logger.LogInformation("Run in {@data}ms", stopwatch.Elapsed.TotalMilliseconds);
            _logger.LogInformation("Req/resp {@request} - {@response}", request, response);
        }

        private async Task<IEnumerable<Rate>> GetRatesAsync(string symbol, DateTime now, CancellationToken cancellationToken)
        {
            var rates = new List<Rate>();

            var call = _marketDataWrapper.CopyRatesRangeStream(
                symbol, now.AddMinutes(-30), now.AddMinutes(5), Timeframe.M1, 5000, cancellationToken);

            await foreach (var rate in call.ResponseStream.ReadAllAsync(cancellationToken: cancellationToken))
                rates.AddRange(rate.Rates);

            return rates;
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
