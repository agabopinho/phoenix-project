﻿using Application.Services.Providers.Cycle;
using Grpc.Core;
using Grpc.Terminal;
using Infrastructure.GrpcServerTerminal;

namespace Application.Services.Providers.Rates
{
    public class InMemoryOnlineRatesProvider : IRatesProvider
    {
        private readonly IMarketDataWrapper _symbolDataWrapper;
        private readonly ICycleProvider _cycleProvider;

        public InMemoryOnlineRatesProvider(
            IMarketDataWrapper symbolDataWrapper,
            ICycleProvider cycleProvider)
        {
            _symbolDataWrapper = symbolDataWrapper;
            _cycleProvider = cycleProvider;
        }

        public SortedList<DateTime, Rate> Rates { get; } = new();

        public async Task CheckNewRatesAsync(
            string symbol,
            DateOnly date,
            TimeSpan timeframe,
            int chunkSize,
            CancellationToken cancellationToken)
        {
            var lastRate = Rates.Values.LastOrDefault();

            var fromDate = lastRate is null ?
                date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) :
                lastRate.Time.ToDateTime();

            using var call = _symbolDataWrapper.StreamRatesFromTicksRange(
                symbol,
                fromDate,
                _cycleProvider.Now().AddSeconds(10),
                timeframe,
                chunkSize,
                cancellationToken);

            await foreach (var reply in call.ResponseStream.ReadAllAsync(cancellationToken: cancellationToken))
            {
                if (!reply.Rates.Any())
                    continue;

                foreach (var rate in reply.Rates)
                    Rates[rate.Time.ToDateTime()] = rate;
            }
        }

        public Task<IEnumerable<Rate>> GetRatesAsync(
            string symbol,
            DateOnly date,
            TimeSpan timeframe,
            TimeSpan window,
            CancellationToken cancellationToken)
            => Task.FromResult<IEnumerable<Rate>>(Rates.Values);

        public async Task<GetSymbolTickReply> GetSymbolTickAsync(string symbol, CancellationToken cancellationToken)
            => await _symbolDataWrapper.GetSymbolTickAsync(symbol, cancellationToken);
    }
}
