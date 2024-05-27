using Application.Services.Providers.Date;
using Grpc.Core;
using Grpc.Terminal;
using Infrastructure.GrpcServerTerminal;

namespace Application.Services.Providers.Rates;

public class InMemoryOnlineRatesProvider(
    IMarketDataWrapper symbolDataWrapper,
    IDateProvider dateProvider) : IRatesProvider
{
    private readonly IMarketDataWrapper _symbolDataWrapper = symbolDataWrapper;
    private readonly IDateProvider _dateProvider = dateProvider;

    public SortedList<DateTime, Rate> Rates { get; } = [];
    public string? Symbol { get; private set; }
    public DateOnly Date { get; private set; }
    public TimeSpan Timeframe { get; private set; }
    public int ChunkSize { get; private set; }

    public void Initialize(string symbol, DateOnly date, TimeSpan timeframe, int chunkSize)
    {
        if (string.IsNullOrWhiteSpace(symbol))
        {
            throw new ArgumentException($"'{nameof(symbol)}' cannot be null or whitespace.", nameof(symbol));
        }

        Symbol = symbol;
        Date = date;
        Timeframe = timeframe;
        ChunkSize = chunkSize;
    }

    public async Task UpdateRatesAsync(CancellationToken cancellationToken)
    {
        CheckInitialized();

        var lastRate = Rates.Values.LastOrDefault();

        var fromDate = lastRate is null ?
            Date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc) :
            lastRate.Time.ToDateTime();

        using var call = _symbolDataWrapper.StreamRatesFromTicksRange(
            Symbol!,
            fromDate,
            _dateProvider.LocalDateSpecifiedUtcKind().AddSeconds(10),
            Timeframe,
            ChunkSize,
            cancellationToken);

        await foreach (var reply in call.ResponseStream.ReadAllAsync(cancellationToken: cancellationToken))
        {
            if (reply.Rates.Count == 0)
            {
                continue;
            }

            foreach (var rate in reply.Rates)
            {
                Rates[rate.Time.ToDateTime()] = rate;
            }
        }
    }

    public Task<IEnumerable<Rate>> GetRatesAsync(CancellationToken cancellationToken)
        => Task.FromResult<IEnumerable<Rate>>(Rates.Values);

    public async Task<GetSymbolTickReply> GetSymbolTickAsync(CancellationToken cancellationToken)
    {
        CheckInitialized();

        return await _symbolDataWrapper.GetSymbolTickAsync(Symbol!, cancellationToken);
    }

    private void CheckInitialized()
    {
        if (string.IsNullOrWhiteSpace(Symbol))
        {
            throw new InvalidOperationException($"'{nameof(Symbol)}' cannot be null or whitespace.");
        }
    }
}
