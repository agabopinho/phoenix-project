using Application.Objects;

namespace Application.Services
{
    public interface IRatesService
    {
        Task<MarketDataResult> GetRatesAsync(string symbol, CancellationToken cancellationToken);
    }
}
