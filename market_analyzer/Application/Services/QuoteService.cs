using Application.Constants;
using Application.Objects;
using StackExchange.Redis;
using System.Text.Json;

namespace Application.Services
{
    public class QuoteService : IQuoteService
    {
        private readonly IDatabase _database;

        public QuoteService(IDatabase database)
        {
            _database = database;
        }

        public async Task<MarketDataResult> GetQuotesAsync(string symbol, CancellationToken _)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentNullException(nameof(symbol));

            var metaValue = await _database.StringGetAsync(GetSymbolMetaKey(symbol));

            if (string.IsNullOrWhiteSpace(metaValue))
                return new MarketDataResult(symbol);

            var ratesInfo = JsonSerializer.Deserialize<QuoteInfo>(metaValue!);
            var allValues = new List<CustomQuote>(Defaults.DefaultListCapacity);

            var key = GetSymbolRatesKey(symbol, ratesInfo!.AvailableRatesTimeframes!.First());
            var list = new List<CustomQuote>(Defaults.DefaultListCapacity);

            await foreach (var value in _database.HashScanAsync(key))
                list.Add(new CustomQuote((double)value.Name, JsonSerializer.Deserialize<decimal[]>(value.Value!)!));

            return new MarketDataResult(
                symbol: symbol,
                info: ratesInfo,
                quotes: list
                    .GroupBy(it => it.Date)
                    .Select(it => it.First())
                    .OrderBy(it => it.Date));
        }

        private static string GetSymbolRatesKey(string symbol, string timeframe)
            => $"mkt:{symbol.ToLower()}:rates:{timeframe.ToLower()}";

        private static string GetSymbolMetaKey(string symbol)
            => $"mkt:{symbol.ToLower()}:meta";
    }
}
