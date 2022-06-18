using Application.Constants;
using Application.Objects;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Application.Services
{
    public class RatesService : IRatesService
    {
        private readonly IDatabase _database;
        private readonly ILogger<RatesService> _logger;

        public RatesService(IDatabase database, ILogger<RatesService> logger)
        {
            _database = database;
            _logger = logger;
        }

        public async Task<MarketDataResult> GetRatesAsync(string symbol, CancellationToken _)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentNullException(nameof(symbol));

            var metaValue = await _database.StringGetAsync(GetSymbolMetaKey(symbol));

            if (string.IsNullOrWhiteSpace(metaValue))
                return new MarketDataResult(symbol);

            var ratesInfo = JsonSerializer.Deserialize<RatesInfo>(metaValue!);
            var allValues = new List<Rate>(Defaults.DefaultListCapacity);

            var key = GetSymbolRatesKey(symbol, ratesInfo!.AvailableRatesTimeframes!.First());
            var list = new List<Rate>(Defaults.DefaultListCapacity);

            await foreach (var value in _database.HashScanAsync(key))
                list.Add(new Rate(JsonSerializer.Deserialize<double[]>(value.Value!)!));

            return new MarketDataResult(
                symbol: symbol,
                ratesInfo: ratesInfo,
                rates: list.OrderByDescending(it => it.Time));
        }

        private static string GetSymbolRatesKey(string symbol, string timeframe)
            => $"mkt:{symbol.ToLower()}:rates:{timeframe.ToLower()}";

        private static string GetSymbolMetaKey(string symbol)
            => $"mkt:{symbol.ToLower()}:meta";
    }
}
