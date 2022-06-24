using Application.Objects;
using Application.Services;

namespace ConsoleApp;

public class BacktestingQuoteService : IQuoteService
{
    public Task<MarketDataResult> GetQuotesAsync(string symbol, CancellationToken cancellationToken)
    {
        var marketDataResult = new MarketDataResult(
            symbol,
            new QuoteInfo
            {
                UpdatedAt = DateTime.Now,
            }, 
            new[]
            {
                new CustomQuote
                {
                    Date = DateTime.Now,
                    Close = 1000
                }
            });

        return Task.FromResult(marketDataResult);
    }
}
