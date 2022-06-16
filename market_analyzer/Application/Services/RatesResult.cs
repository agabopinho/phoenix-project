using System.Text.Json.Serialization;

namespace Application.Services
{
    public class RatesResult
    {
        public RatesResult(string symbol)
        {
            Symbol = symbol.ToUpper();
        }

        public RatesResult(string symbol, Metadata metadata, IEnumerable<Rate> rates)
        {
            Symbol = symbol.ToUpper();
            SetResult(metadata, rates);
        }

        private void SetResult(Metadata metadata, IEnumerable<Rate> rates)
        {
            Metadata = metadata;
            Rates = rates;
            HasResult = true;
        }

        public string Symbol { get; }
        public Metadata? Metadata { get; private set; }
        public IEnumerable<Rate>? Rates { get; private set; }
        public bool HasResult { get; private set; } = false;
    }

    public class Metadata
    {
        [JsonPropertyName("init_at")]
        public double InitAt { get; set; }

        public DateTime InitAtDate => InitAt.ToDateTime();

        [JsonPropertyName("init_count")]
        public double InitCount { get; set; }

        [JsonPropertyName("updated_at")]
        public double UpdatedAt { get; set; }

        public DateTime UpdatedAtDate => UpdatedAt.ToDateTime();

        [JsonPropertyName("updated_count")]
        public double UpdatedCount { get; set; }

        [JsonPropertyName("available_rates_timeframes")]
        public IEnumerable<string>? AvailableRatesTimeframes { get; set; }

        [JsonPropertyName("current_count")]
        public double CurrentCount { get; set; }
    }

    public class Rate
    {
        public Rate(double[] values)
        {
            Time = values[0].ToDateTime();
            Open = values[1];
            High = values[2];
            Low = values[3];
            Close = values[4];
            TickVolume = values[5];
            Spread = values[6];
            RealVolume = values[7];
        }

        public DateTime Time { get; set; }
        public double Open { get; set; }
        public double High { get; set; }
        public double Low { get; set; }
        public double Close { get; set; }
        public double TickVolume { get; set; }
        public double Spread { get; set; }
        public double RealVolume { get; set; }
    }
}
