using Application.Objects;

namespace Application.Services
{
    public interface IQuoteService
    {
        Task<MarketDataResult> GetQuotesAsync(string symbol, CancellationToken cancellationToken);
    }
}
