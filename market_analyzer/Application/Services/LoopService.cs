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
        public static readonly DateOnly Date = new(2022, 6, 29);
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

            var tick = await _marketDataWrapper.GetSymbolTickAsync(Operation.Symbol, cancellationToken);

            var request = _orderCreator.SellAtMarket(
                symbol: Operation.Symbol,
                price: tick.Trade.Bid!.Value,
                volume: 1,
                deviation: 10,
                comment: "test order");

            var response = await _orderManagementWrapper.CheckOrderAsync(request, cancellationToken);

            var histOrders = _orderManagementWrapper.GetHistoryOrdersAsync(group: Operation.Symbol,
                DateTime.UtcNow.AddYears(-5),
                DateTime.UtcNow.AddDays(1),
                cancellationToken);

            var orders = _orderManagementWrapper.GetOrdersAsync(group: Operation.Symbol, cancellationToken: cancellationToken);
            var positions = _orderManagementWrapper.GetPositionsAsync(group: Operation.Symbol, cancellationToken: cancellationToken);

            await Task.WhenAll(histOrders, orders, positions);

            stopwatch.Stop();

            _logger.LogInformation("Run in {@data}ms", stopwatch.Elapsed.TotalMilliseconds);
            _logger.LogInformation("Req/resp {@request} - {@response}", request, response);
        }

        private async Task<IEnumerable<Rate>> GetForexRatesAsync(CancellationToken cancellationToken)
        {
            var rates = new List<Rate>();

            var date = DateTime.SpecifyKind(Operation.Date.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

            var call = _marketDataWrapper.StreamRatesRange(
                symbol: Operation.Symbol,
                utcFromDate: date.AddDays(-1),
                utcToDate: date.AddDays(1),
                timeframe: Timeframe.M1,
                chunkSize: 5000,
                cancellationToken: cancellationToken);

            await foreach (var rate in call.ResponseStream.ReadAllAsync(cancellationToken: cancellationToken))
                rates.AddRange(rate.Rates);

            return rates;
        }

        private async Task<IEnumerable<Rate>> CheckAndGetRatesAsync(CancellationToken cancellationToken)
        {
            await _ratesStateService.CheckNewRatesAsync(
                Operation.Symbol, Operation.Date, Operation.Timeframe,
                Operation.ChunkSize, cancellationToken);

            return await _ratesStateService.GetRatesAsync(
                Operation.Symbol, Operation.Date, Operation.Timeframe,
                TimeSpan.FromMinutes(30), cancellationToken);
        }
    }
}
