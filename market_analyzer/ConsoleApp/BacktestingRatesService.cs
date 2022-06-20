using Application.Objects;
using Application.Services;

namespace ConsoleApp;

public class BacktestingRatesService : IRatesService
{
    public Task<MarketDataResult> GetRatesAsync(string symbol, CancellationToken cancellationToken)
    {
        var marketDataResult = new MarketDataResult(
            symbol,
            new RatesInfo
            {
                UpdatedAt = DateTime.Now,
            }, 
            new[]
            {
                new Rate
                {
                    Time = DateTime.Now,
                    Close = 1000
                }
            });

        return Task.FromResult(marketDataResult);
    }
}
