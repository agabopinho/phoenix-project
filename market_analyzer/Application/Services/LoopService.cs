using Application.Helpers;
using Grpc.Core;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Infrastructure.GrpcServerTerminal;
using Microsoft.Extensions.Logging;
using Skender.Stock.Indicators;
using System.Diagnostics;

namespace Application.Services
{
    public static class Operation
    {
        public static readonly string Symbol = "WINQ22";
        public static readonly DateOnly Date = new(2022, 6, 29);
        public static readonly int ChunkSize = 5000;
        public static readonly TimeSpan Timeframe = TimeSpan.FromSeconds(2);
        public static readonly int Deviation = 10;
        public static readonly long Magic = 467276;
        public static readonly bool ExecOrder = false;
    }

    public interface ILoopService
    {
        Task RunAsync(CancellationToken cancellationToken);
    }

    public class LoopService : ILoopService
    {
        private readonly IRatesStateService _ratesStateService;
        private readonly IMarketDataWrapper _marketDataWrapper;
        private readonly IOrderManagementWrapper _orderManagementWrapper;
        private readonly IOrderCreator _orderCreator;
        private readonly ILogger<ILoopService> _logger;
        private readonly Stopwatch _stopwatch;

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
            _stopwatch = new Stopwatch();
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            _stopwatch.Restart();

            var peddingOrders = await _orderManagementWrapper.GetOrdersAsync(
               group: Operation.Symbol,
               cancellationToken: cancellationToken);

            if (peddingOrders.ResponseCode != Res.SOk ||
                peddingOrders.Orders.Any())
            {
                _stopwatch.Stop();

                _logger.LogInformation("Check Pedding Orders Fail Or Has Pedding Orders, {@total}, in {@data}ms",
                    peddingOrders.Orders.Count,
                    _stopwatch.Elapsed.TotalMilliseconds);

                return;
            }

            await CheckRatesAsync(cancellationToken);

            var rates = await GetRatesAsync(cancellationToken);
            var quotes = ConvertRatesToQuotes(rates).ToArray();
            var indicator = GetVolatilityStopIndicator(quotes).ToArray();

            _stopwatch.Stop();

            await CheckPositionAsync(quotes, indicator, cancellationToken);
        }

        private async Task CheckPositionAsync(Quote[] quotes, VolatilityStopResult[] indicator, CancellationToken cancellationToken)
        {
            var last = indicator[^3..];

            var previous1 = last.Skip(1).First();
            var previous2 = last.First();
            var current = last.Last();

            if ((previous1.UpperBand.HasValue || previous2.UpperBand.HasValue) && current.LowerBand.HasValue)
            {
                var positions = await GetPositionsAsync(cancellationToken);

                if (!positions.Positions.Any())
                {
                    LogLowerBand(quotes, current);

                    await BuyAsync(volume: 1, cancellationToken);
                }
                else if (positions.Positions.Any(it => it.Type == PositionType.Sell))
                {
                    LogLowerBand(quotes, current);

                    var sellPosition = positions.Positions.First(it => it.Type == PositionType.Sell);
                    await BuyAsync(volume: sellPosition.Volume!.Value * 2, cancellationToken);
                }

                return;
            }

            if ((previous1.LowerBand.HasValue || previous2.LowerBand.HasValue) && current.UpperBand.HasValue)
            {
                var positions = await GetPositionsAsync(cancellationToken);

                if (!positions.Positions.Any())
                {
                    LogUpperBand(quotes, current);

                    await SellAsync(volume: 1, cancellationToken);
                }
                else if (positions.Positions.Any(it => it.Type == PositionType.Buy))
                {
                    LogUpperBand(quotes, current);

                    var buyPosition = positions.Positions.First(it => it.Type == PositionType.Buy);
                    await SellAsync(volume: buyPosition.Volume!.Value * 2, cancellationToken);
                }

                return;
            }
        }

        private async Task BuyAsync(double volume, CancellationToken cancellationToken)
        {
            var tick = await _marketDataWrapper.GetSymbolTickAsync(Operation.Symbol, cancellationToken);

            var request = _orderCreator.BuyAtMarket(
                symbol: Operation.Symbol,
                price: tick.Trade.Bid!.Value,
                volume: volume,
                deviation: Operation.Deviation,
                comment: "test order",
                magic: Operation.Magic);

            if (Operation.ExecOrder)
            {
                _logger.LogInformation("Buy Request {@request}", request);
                var response = await _orderManagementWrapper.SendOrderAsync(request, cancellationToken);
                _logger.LogInformation("Buy Reply {@response}", response);
            }
            else
            {
                _logger.LogInformation("Check Buy Request {@request}", request);
                var response = await _orderManagementWrapper.CheckOrderAsync(request, cancellationToken);
                _logger.LogInformation("Check Buy Reply {@response}", response);
            }
        }

        private async Task SellAsync(double volume, CancellationToken cancellationToken)
        {
            var tick = await _marketDataWrapper.GetSymbolTickAsync(Operation.Symbol, cancellationToken);

            var request = _orderCreator.SellAtMarket(
                symbol: Operation.Symbol,
                price: tick.Trade.Ask!.Value,
                volume: volume,
                deviation: 10,
                comment: "test order",
                magic: Operation.Magic);

            if (Operation.ExecOrder)
            {
                _logger.LogInformation("Sell Request {@request}", request);
                var response = await _orderManagementWrapper.SendOrderAsync(request, cancellationToken);
                _logger.LogInformation("Sell Reply {@response}", response);
            }
            else
            {
                _logger.LogInformation("Check Sell Request {@request}", request);
                var response = await _orderManagementWrapper.CheckOrderAsync(request, cancellationToken);
                _logger.LogInformation("Check Sell Reply {@response}", response);
            }
        }

        private async Task<GetPositionsReply> GetPositionsAsync(CancellationToken cancellationToken)
            => await _orderManagementWrapper.GetPositionsAsync(group: Operation.Symbol, cancellationToken: cancellationToken);

        private async Task CheckRatesAsync(CancellationToken cancellationToken)
            => await _ratesStateService.CheckNewRatesAsync(
                Operation.Symbol, Operation.Date, Operation.Timeframe,
                Operation.ChunkSize, cancellationToken);

        private async Task<IEnumerable<Rate>> GetRatesAsync(CancellationToken cancellationToken)
        {
            var result = await _ratesStateService.GetRatesAsync(
                Operation.Symbol, Operation.Date, Operation.Timeframe,
                TimeSpan.FromMinutes(5), cancellationToken);

            return result.OrderBy(it => it.Time);
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

        private static IEnumerable<Quote> ConvertRatesToQuotes(IEnumerable<Rate> rates)
            => rates
            .Where(it => it.Open.HasValue && !double.IsNaN(it.Open.Value))
            .Select(it => new Quote
            {
                Date = it.Time.ToDateTime(),
                Open = Convert.ToDecimal(it.Open),
                High = Convert.ToDecimal(it.High),
                Low = Convert.ToDecimal(it.Low),
                Close = Convert.ToDecimal(it.Close),
                Volume = Convert.ToDecimal(it.Volume),
            });

        private static IEnumerable<VolatilityStopResult> GetVolatilityStopIndicator(IEnumerable<Quote> quotes)
            => quotes.GetVolatilityStop(20, 1.33d);

        private void LogLowerBand(Quote[] quotes, VolatilityStopResult current)
        {
            _logger.LogInformation("LowerBand (Buy): {@band}, in {@data}ms",
                new
                {
                    current.Date,
                    current.LowerBand,
                    quotes.Last().Close
                },
                _stopwatch.Elapsed.TotalMilliseconds);
        }

        private void LogUpperBand(Quote[] quotes, VolatilityStopResult current)
        {
            _logger.LogInformation("UpperBand (Sell): {@band}, in {@data}ms",
                new
                {
                    current.Date,
                    current.UpperBand,
                    quotes.Last().Close
                },
                _stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}
