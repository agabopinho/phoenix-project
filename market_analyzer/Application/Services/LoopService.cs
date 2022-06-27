using Application.Helpers;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Terminal;
using Grpc.Terminal.Enums;
using Infrastructure.Terminal;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Diagnostics;

namespace Application.Services
{
    public static class Operation
    {
        public static readonly string Symbol = "WINQ22";
        public static readonly bool Simulate = false;
        public static readonly DateOnly SimulateDate = new(2022, 6, 27);
        public static readonly int ChunkSize = 5000;
    }

    public class LoopService : ILoopService
    {
        private readonly ITicksService _ticksService;
        private readonly IMarketDataWrapper _marketDataWrapper;

        public LoopService(ITicksService ticksService, IMarketDataWrapper marketDataWrapper)
        {
            _ticksService = ticksService;
            _marketDataWrapper = marketDataWrapper;
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            //await _ticksService.CheckNewTicksAsync(cancellationToken);

            using var call = _marketDataWrapper.CopyRatesRangeStream(
                Operation.Symbol,
                Operation.SimulateDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                Operation.SimulateDate.ToDateTime(new TimeOnly(11, 0, 0), DateTimeKind.Utc),
                Timeframe.M1,
                Operation.ChunkSize,
                cancellationToken);

            var r1 = new List<Rate>();
            await foreach (var result in call.ResponseStream.ReadAllAsync(cancellationToken: cancellationToken))
                r1.AddRange(result.Rates);

            using var call2 = _marketDataWrapper.CopyRatesFromTicksRangeStream(
                Operation.Symbol,
                Operation.SimulateDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc),
                Operation.SimulateDate.ToDateTime(new TimeOnly(11, 1, 0), DateTimeKind.Utc),
                TimeSpan.FromMinutes(1),
                Operation.ChunkSize,
                cancellationToken);

            var r2 = new List<Rate>();
            await foreach (var result in call2.ResponseStream.ReadAllAsync(cancellationToken: cancellationToken))
                r2.AddRange(result.Rates);
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

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            var fromDate = lastTrade is null ?
                Operation.SimulateDate.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) :
                lastTrade.Time.ToDateTime();

            var call = _marketDataWrapper.CopyTicksRangeStream(
                Operation.Symbol,
                fromDate,
                Operation.SimulateDate.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc),
                CopyTicks.Trade,
                Operation.ChunkSize,
                cancellationToken);

            var count = 0;
            var chunkCount = 0;

            await foreach (var reply in call.ResponseStream.ReadAllAsync(cancellationToken: cancellationToken))
            {
                count += reply.Trades.Count;
                chunkCount++;

                var sets = reply.Trades
                    .Where(it => it.Time.ToDateTime() > fromDate)
                    .Select(it =>
                        new SortedSetEntry(it.ToByteArray(), it.Time.ToDateTime().ToTimestamp())
                    ).ToArray();

                if (sets.Any())
                    await _database.SortedSetAddAsync(ticksKey, sets);
            }

            stopwatch.Stop();

            _logger.LogInformation("Count {@total}, chunks: {@chunks}, in {@totalSeconds}ms", count, chunkCount, stopwatch.Elapsed.TotalMilliseconds);
        }

        private async Task<Trade?> GetLastTradeAsync(string key)
        {
            var result = await _database.SortedSetRangeByRankAsync(key, start: 0, stop: 0, order: Order.Descending);

            if (!result.Any())
                return null;

            return Trade.Parser.ParseFrom(result.First()!);
        }

        public static string TicksKey(string symbol, DateOnly simulateDate)
            => $"{symbol.ToLower()}:ticks:{simulateDate:yyyyMMdd}";
    }
}
