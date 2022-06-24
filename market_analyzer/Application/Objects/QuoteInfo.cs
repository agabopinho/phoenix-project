using Application.Infra.Json;
using System.Text.Json.Serialization;

namespace Application.Objects
{
    public class QuoteInfo
    {
        [JsonPropertyName("init_at"), JsonConverter(typeof(TimestampConverter))]
        public DateTime InitAt { get; set; }

        [JsonPropertyName("init_count")]
        public double InitCount { get; set; }

        [JsonPropertyName("updated_at"), JsonConverter(typeof(TimestampConverter))]
        public DateTime UpdatedAt { get; set; }

        [JsonPropertyName("updated_count")]
        public double UpdatedCount { get; set; }

        [JsonPropertyName("available_rates_timeframes")]
        public IEnumerable<string>? AvailableRatesTimeframes { get; set; }

        [JsonPropertyName("current_count")]
        public double CurrentCount { get; set; }
    }

}
