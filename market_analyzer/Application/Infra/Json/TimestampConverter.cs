using Application.Helpers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Application.Infra.Json
{
    public class TimestampConverter : JsonConverter<DateTime>
    {
        public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => reader.GetDouble().ToDateTime();

        public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            => writer.WriteNumberValue(value.ToTimestamp());
    }
}
