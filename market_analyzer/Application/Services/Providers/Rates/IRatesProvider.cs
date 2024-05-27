using Grpc.Terminal;

namespace Application.Services.Providers.Rates;

public interface IRatesProvider
{
    void Initialize(string symbol, DateOnly date, TimeSpan timeframe, int chunkSize);

    Task UpdateRatesAsync(CancellationToken cancellationToken);

    Task<IEnumerable<Rate>> GetRatesAsync(CancellationToken cancellationToken);

    Task<GetSymbolTickReply> GetSymbolTickAsync(CancellationToken cancellationToken);
}
