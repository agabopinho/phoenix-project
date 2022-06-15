using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace Application.Services
{
    public class RatesServices : IRatesService
    {
        private readonly IDatabase _database;
        private readonly ILogger<RatesServices> _logger;

        public RatesServices(IDatabase database, ILogger<RatesServices> logger)
        {
            _database = database;
            _logger = logger;
        }

        public async Task<RatesResult> GetRatesAsync(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
                throw new ArgumentNullException(nameof(symbol));

            var metaValue = await _database.StringGetAsync(GetSymbolMetaKey(symbol));

            if (string.IsNullOrWhiteSpace(metaValue))
                return new RatesResult(symbol);

            var metadata = JsonSerializer.Deserialize<SymbolMetadata>(metaValue!);
            var allValues = new List<Rate>(Defaults.DefaultRateListCapacity);

            var key = GetSymbolRatesKey(symbol, metadata!.AvailableRatesTimeframes!.First());
            var list = new List<Rate>(Defaults.DefaultRateListCapacity);

            await foreach (var value in _database.HashScanAsync(key))
                list.Add(new Rate(JsonSerializer.Deserialize<double[]>(value.Value!)!));

            return new RatesResult(
                symbol: symbol,
                metadata: metadata,
                rates: list.OrderByDescending(it => it.Time));
        }

        private static string GetSymbolRatesKey(string symbol, string timeframe)
            => $"mkt:{symbol.ToLower()}:rates:{timeframe.ToLower()}";

        private static string GetSymbolMetaKey(string symbol)
            => $"mkt:{symbol.ToLower()}:meta";
    }

    public interface IRatesService
    {
        Task<RatesResult> GetRatesAsync(string symbol);
    }
}
