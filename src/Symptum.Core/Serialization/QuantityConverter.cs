using System.Text.Json;
using System.Text.Json.Serialization;
using Symptum.Core.Data;

namespace Symptum.Core.Serialization;

internal class QuantityJsonConverter : JsonConverter<Quantity>
{
    public override Quantity? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? json = reader.GetString();
            if (Quantity.TryParse(json, out Quantity? quantity))
                return quantity;
        }

        return null;
    }

    public override void Write(Utf8JsonWriter writer, Quantity value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value?.ToString());
    }
}
