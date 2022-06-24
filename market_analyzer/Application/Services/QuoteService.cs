using Application.Objects;
using Infrastructure.Platform;

namespace Application.Services
{
    public class QuoteService : IQuoteService
    {
        public QuoteService()
        {
        }

        public async Task<MarketDataResult> GetQuotesAsync(string symbol, CancellationToken cancellationToken)
        {
            var a = new MarketDataWrapper();

            await a.CopyTicksRangeAsync(cancellationToken);

            return new MarketDataResult(symbol);
        }
    }
}
