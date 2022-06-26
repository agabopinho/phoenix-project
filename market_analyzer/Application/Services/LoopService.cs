using Application.Helpers;
using Grpc.Core;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Infrastructure.Terminal;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Services
{
    public static class Operation
    {
        public static readonly string Symbol = "WINQ22";
        public static readonly bool Simulate = false;
        public static readonly DateOnly SimulateDate = new(2022, 6, 24);
    }

    public class LoopService : ILoopService
    {
        private readonly ITicksService _ticksService;
        private readonly ILogger<LoopService> _logger;

        public LoopService(ITicksService ticksService, ILogger<LoopService> logger)
        {
            _ticksService = ticksService;
            _logger = logger;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            await _ticksService.CheckNewTicksAsync(cancellationToken);
        }
    }

    public interface ITicksService
    {
        Task CheckNewTicksAsync(CancellationToken cancellationToken);
    }

    public class TicksService : ITicksService
    {
        private readonly IDatabase _database;
        private readonly IMarketDataWrapper _marketDataWrapper;
        private readonly ILogger<TicksService> _logger;

        public TicksService(IDatabase database, IMarketDataWrapper marketDataWrapper, ILogger<TicksService> logger)
        {
            _database = database;
            _marketDataWrapper = marketDataWrapper;
            _logger = logger;
        }

        public async Task CheckNewTicksAsync(CancellationToken cancellationToken)
        {
            var ticksKey = TicksKey(Operation.Symbol, Operation.SimulateDate);
            var lastTrade = await GetLastTradeAsync(ticksKey);

            _logger.LogInformation("Started.");

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var call = _marketDataWrapper.CopyTicksRangeStream(
                Operation.Symbol,
                lastTrade is null ? Operation.SimulateDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) : lastTrade.Time.ToDateTime(),
                Operation.SimulateDate.ToDateTime(new TimeOnly(20, 00), DateTimeKind.Utc),
                CopyTicks.Trade,
                chunkSize: 10000,
                cancellationToken: cancellationToken);

            var count = 0;
            var chunkCount = 0;

            await foreach (var reply in call.ResponseStream.ReadAllAsync(cancellationToken: cancellationToken))
            {
                count += reply.Trades.Count;
                chunkCount++;
            }

            stopwatch.Stop();

            _logger.LogInformation("Count {@total}, chunks: {@chunks}, in seconds {@totalSeconds}", count, chunkCount, stopwatch.Elapsed.TotalSeconds);
        }

        private async Task<Trade?> GetLastTradeAsync(string key)
        {
            var result = await _database.SortedSetRangeByRankAsync(key, start: 0, stop: 0, order: Order.Descending);

            if (!result.Any())
                return null;

            return JsonSerializer.Deserialize<Trade>(result.First()!);
        }

        public static string TicksKey(string symbol, DateOnly simulateDate)
            => $"{symbol.ToLower()}:ticks:{simulateDate:yyyyMMdd}";
    }
}
