using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CuriousContraptions;

/// <summary>Exact diagnostic wire names only: no case folding, whitespace aliases or combined names.</summary>
public abstract class ExactPlaytestEnumConverter<T> : JsonConverter<T> where T : struct, Enum
{
    private static readonly Dictionary<string, T> FromWire = new(StringComparer.Ordinal);
    private static readonly Dictionary<T, string> ToWire = new();
    static ExactPlaytestEnumConverter()
    {
        foreach (var value in Enum.GetValues<T>())
        {
            // Enum-to-string conversion belongs only at this serialization boundary.
            var name = JsonNamingPolicy.SnakeCaseLower.ConvertName(value.ToString());
            FromWire.Add(name, value);
            ToWire.Add(value, name);
        }
    }
    public override T Read(ref Utf8JsonReader reader, Type type, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String ||
            !FromWire.TryGetValue(reader.GetString()!, out var value))
            throw new JsonException("Unknown diagnostic enum wire value.");
        return value;
    }
    public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
    {
        if (!ToWire.TryGetValue(value, out var name))
            throw new JsonException("Undefined diagnostic enum value.");
        writer.WriteStringValue(name);
    }
}
