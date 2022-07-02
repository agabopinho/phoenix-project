using Grpc.Terminal;

namespace Application.Services.Providers.Rates
{
    public interface IRatesProvider
    {
        Task CheckNewRatesAsync(
            string symbol, DateOnly date, TimeSpan timeframe,
            int chunkSize, CancellationToken cancellationToken);

        Task<IEnumerable<Rate>> GetRatesAsync(
            string symbol, DateOnly date, TimeSpan timeframe,
            TimeSpan window, CancellationToken cancellationToken);

        Task<GetSymbolTickReply> GetSymbolTickAsync(string symbol, CancellationToken cancellationToken);
    }
}
