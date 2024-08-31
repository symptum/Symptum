using System.Text.Json.Serialization;
using System.Text.Json;
using Symptum.Core.Data;

namespace Symptum.Core.Serialization;

internal class AuthorInfoConverter : JsonConverter<AuthorInfo>
{
    public override AuthorInfo Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            string? json = reader.GetString();
            if (AuthorInfo.TryParse(json, out AuthorInfo author))
                return author;
        }

        return default;
    }

    public override void Write(Utf8JsonWriter writer, AuthorInfo value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}
