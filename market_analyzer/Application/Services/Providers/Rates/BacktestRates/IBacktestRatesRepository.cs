namespace Application.Services.Providers.Rates.BacktestRates
{
    public interface IBacktestRatesRepository
    {
        Task<IEnumerable<TickData>> GetTicksAsync(string symbol, DateOnly date, CancellationToken cancellationToken);
    }
}
