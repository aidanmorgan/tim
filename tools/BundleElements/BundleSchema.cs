using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Fluid;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.BundleElements;

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

public sealed record MechanicalPayload(KinematicMode KinematicMode, RigidBodyMassProperties MassProperties,
    IReadOnlyList<AnchorPointDescriptor> AnchorPoints);

public sealed record ElectricalPayload(IReadOnlyList<ElectricalNodeDescriptor> Nodes);

public sealed record SignalPayload(IReadOnlyList<SignalTriggerDescriptor> Triggers, SignalLatchMode LatchMode, SignalCounterDescriptor? Counter);

public sealed record FluidPayload(FluidProperties Properties, IReadOnlyList<FluidPortDescriptor> Ports);

/// <summary>Writes and reads the bundle. f32 values are written in shortest round-trip form, vectors as [x, y, z] and quaternions as
/// [x, y, z, w]; enums are written by name, and reading rejects an unknown name, a number in place of a name, an unknown field, a
/// missing field and a null where a value is required.</summary>
public static class BundleSchema
{
    public static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        NewLine = "\n",
        UnmappedMemberHandling = JsonUnmappedMemberHandling.Disallow,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false), new Vector3Converter(), new QuaternionConverter() }
    };

    public static BundleEntry Entry(IPuzzleElementDeclaration declaration)
    {
        ArgumentNullException.ThrowIfNull(declaration);
        return new(declaration.Metadata, Domains(declaration.SupportedDomains), declaration.Footprint, declaration.Ports,
            declaration.Colliders, declaration.Events,
            declaration is IMechanicalElement mechanical
                ? new MechanicalPayload(mechanical.KinematicMode, mechanical.MassProperties, mechanical.AnchorPoints) : null,
            declaration is IElectricalElement electrical ? new ElectricalPayload(electrical.ElectricalNodes) : null,
            declaration is ISignalElement signal ? new SignalPayload(signal.Triggers, signal.LatchMode, signal.Counter) : null,
            declaration is IFluidElement fluid ? new FluidPayload(fluid.FluidProperties, fluid.FluidPorts) : null);
    }

    /// <summary>The bundle text for these declarations, ordered by ordinal element id.</summary>
    public static string Write(IEnumerable<IPuzzleElementDeclaration> declarations)
    {
        ArgumentNullException.ThrowIfNull(declarations);
        var entries = declarations.Select(Entry).OrderBy(entry => entry.Metadata.Id, StringComparer.Ordinal).ToArray();
        return JsonSerializer.Serialize(new BundleDocument(entries), Options) + "\n";
    }

    public static BundleDocument Read(string json) =>
        JsonSerializer.Deserialize<BundleDocument>(json, Options) ?? throw new JsonException("The bundle is null.");

    /// <summary>The declared domains one flag each, in ascending flag order.</summary>
    public static IReadOnlyList<SimulationDomain> Domains(SimulationDomain domains) =>
        Enum.GetValues<SimulationDomain>().Where(flag => flag != SimulationDomain.None && domains.HasFlag(flag)).ToArray();

    private sealed class Vector3Converter : JsonConverter<Vector3>
    {
        public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Span<float> values = stackalloc float[3];
            ReadComponents(ref reader, values);
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

    private sealed class QuaternionConverter : JsonConverter<Quaternion>
    {
        public override Quaternion Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            Span<float> values = stackalloc float[4];
            ReadComponents(ref reader, values);
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

    /// <summary>Reads exactly <paramref name="values"/>.Length numbers as one array.</summary>
    private static void ReadComponents(ref Utf8JsonReader reader, scoped Span<float> values)
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
