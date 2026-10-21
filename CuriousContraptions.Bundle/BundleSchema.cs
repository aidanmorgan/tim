using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Fluid;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.Bundle;

/// <summary>The element bundle: one schema, every package's declaration, ordered by element id. It carries no timestamp, so the same
/// packages always produce the same bytes.</summary>
public sealed record BundleDocument(IReadOnlyList<BundleEntry> Elements);

/// <summary>One element's complete declaration. A domain payload is present exactly when the element declares that domain's
/// contract.</summary>
public sealed record BundleEntry(
    ElementMetadata Metadata,
    IReadOnlyList<SimulationDomain> Domains,
    ElementFootprint Footprint,
    IReadOnlyList<PortDescriptor> Ports,
    IReadOnlyList<ColliderDescriptor> Colliders,
    IReadOnlyList<EventDescriptor> Events,
    MechanicalPayload? Mechanical,
    ElectricalPayload? Electrical,
    SignalPayload? Signal,
    FluidPayload? Fluid);

/// <summary>The mechanical payload. <see cref="Sliders"/> is empty unless the element is an <see cref="ISliderElement"/>.</summary>
public sealed record MechanicalPayload(KinematicMode KinematicMode, RigidBodyMassProperties MassProperties,
    IReadOnlyList<AnchorPointDescriptor> AnchorPoints, IReadOnlyList<PrismaticSliderDescriptor> Sliders);

public sealed record ElectricalPayload(IReadOnlyList<ElectricalNodeDescriptor> Nodes);

public sealed record SignalPayload(IReadOnlyList<SignalTriggerDescriptor> Triggers, SignalLatchMode LatchMode, SignalCounterDescriptor? Counter);

public sealed record FluidPayload(FluidProperties Properties, IReadOnlyList<FluidPortDescriptor> Ports);

/// <summary>Writes and reads the bundle. f32 values are written in shortest round-trip form, vectors as [x, y, z] and quaternions as
/// [x, y, z, w]; enums are written by name, and reading rejects an unknown name, a number in place of a name, an unknown field, a
/// missing field and a null where a value is required. Serialisation is source generated, so the trimmed game reads the same schema
/// the generator writes.</summary>
public static class BundleSchema
{
    public static BundleEntry Entry(IPuzzleElementDeclaration declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        return new(declaration.Metadata, Domains(declaration.SupportedDomains), declaration.Footprint, declaration.Ports,
            declaration.Colliders, declaration.Events,
            declaration is IMechanicalElement mechanical
                ? new MechanicalPayload(mechanical.KinematicMode, mechanical.MassProperties, mechanical.AnchorPoints,
                    mechanical is ISliderElement slider ? slider.Sliders : []) : null,
            declaration is IElectricalElement electrical ? new ElectricalPayload(electrical.ElectricalNodes) : null,
            declaration is ISignalElement signal ? new SignalPayload(signal.Triggers, signal.LatchMode, signal.Counter) : null,
            declaration is IFluidElement fluid ? new FluidPayload(fluid.FluidProperties, fluid.FluidPorts) : null);
    }

    /// <summary>The bundle text for these declarations, ordered by ordinal element id.</summary>
    public static string Write(IEnumerable<IPuzzleElementDeclaration> declarations)
    {
        ArgumentNullException.ThrowIfNull(declarations);
        var entries = declarations.Select(Entry).OrderBy(entry => entry.Metadata.Id, StringComparer.Ordinal).ToArray();
        return JsonSerializer.Serialize(new BundleDocument(entries), BundleJsonContext.Default.BundleDocument) + "\n";
    }

    /// <summary>The bundle these bytes hold. Malformed input throws <see cref="JsonException"/>.</summary>
    public static BundleDocument Read(string json) =>
        JsonSerializer.Deserialize(json, BundleJsonContext.Default.BundleDocument) ?? throw new JsonException("The bundle is null.");

    /// <summary>The declared domains one set bit each, in ascending bit order. An undefined bit is kept, so validation rejects it.</summary>
    public static IReadOnlyList<SimulationDomain> Domains(SimulationDomain domains) =>
        Enumerable.Range(0, 32).Select(bit => (SimulationDomain)(1u << bit)).Where(flag => (domains & flag) != 0).ToArray();
}

/// <summary>The bundle's source-generated serialisation contract. Every enum the schema reaches has its exact-name converter here
/// (BundleSchemaTests checks the list against the schema's type graph).</summary>
[JsonSourceGenerationOptions(
    WriteIndented = true,
    NewLine = "\n",
    UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true,
    Converters =
    [
        typeof(Vector3Converter), typeof(QuaternionConverter),
        typeof(ExactEnumConverter<ElementCategory>), typeof(ExactEnumConverter<ParameterUnit>), typeof(ExactEnumConverter<ParameterKind>),
        typeof(ExactEnumConverter<SimulationDomain>), typeof(ExactEnumConverter<PortDirection>), typeof(ExactEnumConverter<PortSignalKind>),
        typeof(ExactEnumConverter<ColliderShapeKind>), typeof(ExactEnumConverter<SimulationEventKind>), typeof(ExactEnumConverter<KinematicMode>),
        typeof(ExactEnumConverter<ElectricalRole>), typeof(ExactEnumConverter<SignalTriggerMode>), typeof(ExactEnumConverter<SignalLatchMode>),
        typeof(ExactEnumConverter<FluidPortRole>)
    ])]
[JsonSerializable(typeof(BundleDocument))]
internal sealed partial class BundleJsonContext : JsonSerializerContext;

/// <summary>Enums by exact member name only: a number, a different case, surrounding space or a comma-joined flag list is rejected on
/// read, and a value with no single member name is rejected on write.</summary>
internal sealed class ExactEnumConverter<TEnum> : JsonConverter<TEnum> where TEnum : struct, Enum
{
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String) throw new JsonException($"Expected a {typeof(TEnum).Name} name.");
        var name = reader.GetString();
        if (name is null || !Enum.GetNames<TEnum>().Contains(name, StringComparer.Ordinal))
            throw new JsonException($"'{name}' is not a {typeof(TEnum).Name} name.");
        return Enum.Parse<TEnum>(name, ignoreCase: false);
    }

    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options) =>
        writer.WriteStringValue(Enum.GetName(value) ?? throw new JsonException($"{typeof(TEnum).Name} {value} has no member name."));
}

internal sealed class Vector3Converter : JsonConverter<Vector3>
{
    public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Span<float> values = stackalloc float[3];
        Components.Read(ref reader, values);
        return new(values[0], values[1], values[2]);
    }

    public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Z);
        writer.WriteEndArray();
    }
}

internal sealed class QuaternionConverter : JsonConverter<Quaternion>
{
    public override Quaternion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        Span<float> values = stackalloc float[4];
        Components.Read(ref reader, values);
        return new(values[0], values[1], values[2], values[3]);
    }

    public override void Write(Utf8JsonWriter writer, Quaternion value, JsonSerializerOptions options)
    {
        writer.WriteStartArray();
        writer.WriteNumberValue(value.X);
        writer.WriteNumberValue(value.Y);
        writer.WriteNumberValue(value.Z);
        writer.WriteNumberValue(value.W);
        writer.WriteEndArray();
    }
}

internal static class Components
{
    /// <summary>Reads exactly <paramref name="values"/>.Length numbers as one array.</summary>
    public static void Read(ref Utf8JsonReader reader, scoped Span<float> values)
    {
        if (reader.TokenType != JsonTokenType.StartArray) throw new JsonException($"Expected an array of {values.Length} numbers.");
        for (var i = 0; i < values.Length; i++)
        {
            if (!reader.Read() || reader.TokenType != JsonTokenType.Number)
                throw new JsonException($"Expected an array of {values.Length} numbers.");
            values[i] = reader.GetSingle();
        }
        if (!reader.Read() || reader.TokenType != JsonTokenType.EndArray)
            throw new JsonException($"Expected an array of {values.Length} numbers.");
    }
}
