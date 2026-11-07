using System.Text.Json;
using System.Text.Json.Serialization;
using CuriousContraptions.Abstractions.Electrical;

namespace CuriousContraptions.Bundle;

internal sealed class LocalSupplyNodeIdConverter : JsonConverter<LocalSupplyNodeId>
{
    public override LocalSupplyNodeId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String || string.IsNullOrWhiteSpace(reader.GetString()))
            throw new JsonException("A supply node requires an identity.");
        return new(reader.GetString()!);
    }
    public override void Write(Utf8JsonWriter writer, LocalSupplyNodeId value, JsonSerializerOptions options)
    {
        if (value == default) throw new JsonException("A supply node requires an identity.");
        writer.WriteStringValue(value.Value);
    }
}

internal sealed class LocalContactWorkIdConverter : JsonConverter<LocalContactWorkId>
{
    public override LocalContactWorkId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String || string.IsNullOrWhiteSpace(reader.GetString()))
            throw new JsonException("A contact-work store requires an identity.");
        return new(reader.GetString()!);
    }
    public override void Write(Utf8JsonWriter writer, LocalContactWorkId value, JsonSerializerOptions options)
    {
        if (value == default) throw new JsonException("A contact-work store requires an identity.");
        writer.WriteStringValue(value.Value);
    }
}

