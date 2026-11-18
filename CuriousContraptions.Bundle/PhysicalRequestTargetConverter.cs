using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using CuriousContraptions.Gpu;

namespace CuriousContraptions.Bundle;

/// <summary>The required tagged request union owns its zero inactive lanes; standalone IDs remain nonzero.</summary>
internal sealed class PhysicalRequestTargetConverter : JsonConverter<LocalPhysicalRequestTarget>
{
    private static readonly ExactEnumConverter<PhysicalRequestTargetKind> KindConverter = new();
    public override LocalPhysicalRequestTarget Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject) throw new JsonException("A request target is an object.");
        PhysicalRequestTargetKind kind = default;
        uint retention = 0, actuator = 0;
        var fields = 0;
        while (reader.Read() && reader.TokenType != JsonTokenType.EndObject)
        {
            if (reader.TokenType != JsonTokenType.PropertyName) throw new JsonException("Invalid request target field.");
            var field = reader.GetString() switch { "Kind" => 1, "Retention" => 2, "Actuator" => 4, _ => 0 };
            if (field == 0 || (fields & field) != 0 || !reader.Read()) throw new JsonException("Unknown or duplicate request target field.");
            fields |= field;
            if (field == 1) kind = KindConverter.Read(ref reader, typeof(PhysicalRequestTargetKind), options);
            else
            {
                if (reader.TokenType != JsonTokenType.Number || !reader.TryGetUInt32(out var id))
                    throw new JsonException("A request identity lane is an unsigned integer.");
                if (field == 2) retention = id; else actuator = id;
            }
        }
        var target = new LocalPhysicalRequestTarget(kind, new(retention), new(actuator));
        if (reader.TokenType != JsonTokenType.EndObject || fields != 7 || !target.IsValid)
            throw new JsonException("Request target lanes do not match their exact kind.");
        return target;
    }
    public override void Write(Utf8JsonWriter writer, LocalPhysicalRequestTarget value, JsonSerializerOptions options)
    {
        if (!value.IsValid) throw new JsonException("Invalid request target.");
        writer.WriteStartObject();
        writer.WritePropertyName("Kind"); KindConverter.Write(writer, value.Kind, options);
        writer.WriteNumber("Retention", value.Retention.Value);
        writer.WriteNumber("Actuator", value.Actuator.Value);
        writer.WriteEndObject();
    }
}
