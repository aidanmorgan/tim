using System.Text.Json;
using System.Text.Json.Serialization;
using CuriousContraptions.Presentation;

namespace CuriousContraptions.Bundle;

/// <summary>Explicit construction preserves the authored peak instead of default-constructing a readonly value.</summary>
internal sealed class AnimationPeakPhaseConverter : JsonConverter<AnimationPeakPhase>
{
    public override AnimationPeakPhase Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.StartObject || !reader.Read() ||
            reader.TokenType != JsonTokenType.PropertyName || reader.GetString() != "Value" || !reader.Read() ||
            reader.TokenType != JsonTokenType.Number || !reader.TryGetSingle(out var value) ||
            !float.IsFinite(value) || value < 0 || value > 1 || !reader.Read() || reader.TokenType != JsonTokenType.EndObject)
            throw new JsonException("An impulse peak requires one finite Value in [0, 1].");
        return new(value);
    }

    public override void Write(Utf8JsonWriter writer, AnimationPeakPhase value, JsonSerializerOptions options)
    {
        if (!float.IsFinite(value.Value) || value.Value < 0 || value.Value > 1)
            throw new JsonException("An impulse peak must be in [0, 1].");
        writer.WriteStartObject();
        writer.WriteNumber("Value", value.Value);
        writer.WriteEndObject();
    }
}
