using System.Collections;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Electrical;
using CuriousContraptions.Abstractions.Fluid;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;

namespace CuriousContraptions.BundleElements.Tests;

/// <summary>Story 9A.4 AC3 and AC5: every package's declaration equals its deserialised bundle entry, f32 bit for bit, and the
/// bundle is deterministic and typed.</summary>
public sealed class BundleRoundTripTests
{
    public static TheoryData<string> Packages() => [.. PackageDiscovery.PackageNames(TestRepository.Root)];

    private static IPuzzleElementDeclaration Declaration(string package)
    {
        var discovered = PackageDiscovery.Discover(TestRepository.Root);
        Assert.Empty(discovered.Errors);
        return discovered.Packages.Single(entry => entry.Package == package).Declaration;
    }

    /// <summary>The bundle text of the declaration's entry before the scene bake, which only adds colliders and tube mouths.</summary>
    private static string Write(IPuzzleElementDeclaration declaration) => BundleSchema.WriteEntries([BundleSchema.Entry(declaration)]);

    [Theory]
    [MemberData(nameof(Packages))]
    public void EveryPackageEqualsItsDeserialisedEntry(string package)
    {
        var declaration = Declaration(package);
        var entry = Assert.Single(BundleSchema.Read(Write(declaration)).Elements);
        AssertEntry(declaration, entry);
    }

    // engine-revolute-drive: a hinge's travel is a closed enum written by its exact member name, like every other bundle enum; its
    // number is refused on read.
    [Fact]
    public void AHingeTravelIsWrittenByNameAndItsNumberIsRefused()
    {
        var json = Write(Declaration("ImpactLever"));

        Assert.Contains("\"Travel\": \"Limited\"", json, StringComparison.Ordinal);
        Assert.Throws<JsonException>(() => BundleSchema.Read(json.Replace("\"Travel\": \"Limited\"", "\"Travel\": 0", StringComparison.Ordinal)));
    }

    [Theory]
    [InlineData("ConstraintKind", "NonPositiveVelocity")]
    [InlineData("Request", "Release")]
    public void RetentionEnumsUseExactNamesAndRefuseNumericOrUnknownValues(string field, string name)
    {
        var json = Write(Declaration("WoundSpring"));
        var canonical = System.Text.Json.JsonSerializer.Serialize(field) + ": " + System.Text.Json.JsonSerializer.Serialize(name);
        Assert.Contains(canonical, json, StringComparison.Ordinal);
        var decoded = Assert.Single(BundleSchema.Read(json).Elements);
        Assert.Equal(TransmissionConstraintKind.NonPositiveVelocity, Assert.Single(decoded.Mechanical!.Transmissions).ConstraintKind);
        Assert.Equal(PhysicalRequestOperation.Release, Assert.IsType<SignalPhysicalRequestDescriptor>(Assert.Single(decoded.Signal!.Nodes)).Request);
        foreach (var invalid in new[] { "0", "\"Unknown\"", "\"" + name.ToLowerInvariant() + "\"" })
            Assert.Throws<JsonException>(() => BundleSchema.Read(json.Replace(canonical,
                System.Text.Json.JsonSerializer.Serialize(field) + ": " + invalid, StringComparison.Ordinal)));
    }

    [Fact]
    public void LocalPrismaticRetentionIdSerializesAsScalarAndRefusesZeroOrObjects()
    {
        var json = Write(Declaration("WoundSpring"));
        using var document = JsonDocument.Parse(json);
        var element = document.RootElement.GetProperty("Elements")[0];
        var retention = element.GetProperty("Mechanical").GetProperty("Retentions")[0];
        Assert.Equal(JsonValueKind.Number, retention.GetProperty("Id").ValueKind);
        Assert.Equal(1u, retention.GetProperty("Id").GetUInt32());
        Assert.Contains("\"Retention\": 1", json, StringComparison.Ordinal);
        foreach (var invalid in new[] { "0", "{\"Value\": 1}" })
            Assert.Throws<JsonException>(() => BundleSchema.Read(json.Replace("\"Retention\": 1", "\"Retention\": " + invalid, StringComparison.Ordinal)));
        Assert.Throws<JsonException>(() => BundleSchema.WriteEntries([BundleSchema.Read(json).Elements[0] with { Mechanical = BundleSchema.Read(json).Elements[0].Mechanical! with { Retentions = [new(new(0), new(CoordinateKind.Prismatic, 0), new(1))] } }]));
    }


    [Theory]
    [InlineData("WoundSpring", CuriousContraptions.Gpu.PhysicalRequestTargetKind.Retention)]
    [InlineData("Cannon", CuriousContraptions.Gpu.PhysicalRequestTargetKind.FiniteWorkActuator)]
    public void PhysicalRequestUnionOwnsRequiredZeroInactiveLanes(string package, CuriousContraptions.Gpu.PhysicalRequestTargetKind expected)
    {
        var json = Write(Declaration(package));
        var target = Assert.IsType<SignalPhysicalRequestDescriptor>(Assert.Single(BundleSchema.Read(json).Elements[0].Signal!.Nodes)).Target;
        Assert.Equal(expected, target.Kind);
        Assert.True(target.IsValid);
        using var document = JsonDocument.Parse(json);
        var row = document.RootElement.GetProperty("Elements")[0].GetProperty("Signal").GetProperty("Nodes")[0].GetProperty("Target");
        Assert.Equal(JsonValueKind.Number, row.GetProperty("Retention").ValueKind);
        Assert.Equal(JsonValueKind.Number, row.GetProperty("Actuator").ValueKind);
        Assert.Equal(expected == CuriousContraptions.Gpu.PhysicalRequestTargetKind.Retention ? 1u : 0u, row.GetProperty("Retention").GetUInt32());
        Assert.Equal(expected == CuriousContraptions.Gpu.PhysicalRequestTargetKind.FiniteWorkActuator ? 1u : 0u, row.GetProperty("Actuator").GetUInt32());
        var kind = JsonSerializer.Serialize(row.GetProperty("Kind").GetString());
        var retention = row.GetProperty("Retention").GetUInt32();
        var actuator = row.GetProperty("Actuator").GetUInt32();
        var prefix = $"{{\"Kind\":{kind},\"Retention\":{retention},\"Actuator\":{actuator}";
        string[] refused =
        [
            $"{{\"Retention\":{retention},\"Actuator\":{actuator}}}",
            $"{{\"Kind\":{kind},\"Actuator\":{actuator}}}",
            $"{{\"Kind\":{kind},\"Retention\":{retention}}}",
            prefix + ",\"Extra\":0}",
            prefix + ",\"Retention\":0}",
            $"{{\"Kind\":{kind},\"Retention\":1,\"Actuator\":1}}",
            "{\"Kind\":0,\"Retention\":1,\"Actuator\":0}",
            "{\"Kind\":\"Unknown\",\"Retention\":1,\"Actuator\":0}"
        ];
        foreach (var invalid in refused)
            Assert.Throws<JsonException>(() => BundleSchema.Read(json.Replace(row.GetRawText(), invalid, StringComparison.Ordinal)));
    }

    // Guard: every enum the bundle schema's type graph reaches (from BundleDocument through public properties, collection elements and
    // nullable values) has an exact-name converter in the source-generated context, so no enum is written as a number.
    [Fact]
    public void GuardEveryEnumInTheBundleSchemaHasAnExactNameConverter()
    {
        var context = typeof(BundleSchema).Assembly.GetType("CuriousContraptions.Bundle.BundleJsonContext", throwOnError: true)!;
        var options = context.GetCustomAttribute<System.Text.Json.Serialization.JsonSourceGenerationOptionsAttribute>()!;
        var converted = options.Converters!.Where(type => type.IsGenericType && type.GetGenericTypeDefinition().Name.StartsWith("ExactEnumConverter", StringComparison.Ordinal))
            .Select(type => type.GetGenericArguments()[0]).ToHashSet();
        var reached = new HashSet<Type>();
        var pending = new Stack<Type>([typeof(BundleDocument)]);
        while (pending.TryPop(out var type))
        {
            if (!reached.Add(type)) continue;
            if (type.IsEnum) continue;
            if (type.IsArray) pending.Push(type.GetElementType()!);
            foreach (var argument in type.IsGenericType ? type.GetGenericArguments() : []) pending.Push(argument);
            if (type.Namespace?.StartsWith("CuriousContraptions", StringComparison.Ordinal) == true)
                {
                    foreach (var derived in type.GetCustomAttributes<System.Text.Json.Serialization.JsonDerivedTypeAttribute>()) pending.Push(derived.DerivedType);
                    foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
                        if (property.GetCustomAttribute<System.Text.Json.Serialization.JsonIgnoreAttribute>() is null) pending.Push(property.PropertyType);
                }
        }
        var enums = reached.Where(type => type.IsEnum).ToArray();

        Assert.Contains(typeof(RevoluteTravel), enums); // guard: the walk reaches the hinge descriptor
        Assert.Empty(enums.Where(type => !converted.Contains(type)).Select(type => type.FullName));
    }

    [Fact]
    public void SignedZeroAndSubnormalValuesRoundTripBitForBit()
    {
        var subnormal = BitConverter.Int32BitsToSingle(1);
        var fixture = new Fixture
        {
            MassFrame = MassFrameDeclaration.Constant(-0f, new(subnormal, -0f, 3.4028235e38f), new(1f / 3f, 0.1f, 16777217f), Quaternion.Identity, -subnormal, new(0, -9.81f, 0))
        };
        var written = FixtureBake.Entry(fixture, [new(ColliderShapeKind.Box, new(-0f, subnormal, .1f), new(float.MaxValue, -subnormal, float.Epsilon),
            new(-0f, 0f, 0f, 1f), new(-0f, subnormal, 0f, 1e-7f), 0f)], []);
        var json = BundleSchema.WriteEntries([written]);
        Assert.Contains("-0", json, StringComparison.Ordinal);
        Assert.Contains("1E-45", json, StringComparison.Ordinal);
        var read = Assert.Single(BundleSchema.Read(json).Elements);
        AssertEntry(fixture, read);
        AssertSame(written.Colliders, read.Colliders, "Colliders");
    }

    [Fact]
    public void ACylinderColliderRoundTripsByItsEnumName()
    {
        // Story 10.1: the bundle's closed shape set carries Cylinder (r, h, r) by name, bit for bit.
        var fixture = new Fixture();
        var written = FixtureBake.Entry(fixture,
            [new(ColliderShapeKind.Cylinder, new(.4f, .085f, .4f), new(0f, .5f, 0f), Quaternion.Identity, new(.1f, .3f, .02f, .1f), 0f)], []);
        var json = BundleSchema.WriteEntries([written]);
        Assert.Contains("\"ShapeKind\": \"Cylinder\"", json, StringComparison.Ordinal);
        var entry = Assert.Single(BundleSchema.Read(json).Elements);
        AssertEntry(fixture, entry);
        AssertSame(written.Colliders, entry.Colliders, "Colliders");
        Assert.Equal(ColliderShapeKind.Cylinder, Assert.Single(entry.Colliders).ShapeKind);
        Assert.Throws<JsonException>(() => BundleSchema.Read(json.Replace("\"Cylinder\"", "\"cylinder\"", StringComparison.Ordinal)));
    }

    [Fact]
    public void ASliderRoundTripsBitForBit()
    {
        var fixture = new Fixture { Parameters = Fixture.SpringParameters, Sliders = [Fixture.Slider() with { BodiesCollide = true }] };
        var written = FixtureBake.Entry(fixture);
        var entry = Assert.Single(BundleSchema.Read(BundleSchema.WriteEntries([written])).Elements);
        AssertEntry(fixture, entry);
        Assert.Single(entry.Mechanical!.Sliders);
        AssertSame(written.Mechanical!.MovingBodyColliders, entry.Mechanical.MovingBodyColliders, "MovingBodyColliders");
    }

    /// <summary>The baked moving-body collider lists, one per moving body in package body order (sliders, then hinges), are a required
    /// list of lists that reads back in order, bit for bit: a slider's plate and a hinge's sphere keep their bodies. A null inner list,
    /// which the nullable annotations cannot refuse inside a list, and a payload without the key are refused on read. Control: the
    /// same entry reads back equal.</summary>
    [Fact]
    public void MovingBodyColliderListsRoundTripInBodyOrderAndANullListIsRefused()
    {
        var hinge = ((IHingeElement)Declaration("ImpactLever")).Hinges[0];
        var sphere = new ColliderDescriptor(ColliderShapeKind.Sphere, new(.2f), new(.3f, 0f, 0f), Quaternion.Identity, new(.5f, .2f, .01f, .1f), 0f);
        var fixture = new Fixture { Parameters = Fixture.SpringParameters, Sliders = [Fixture.Slider()] };
        var written = FixtureBake.Entry(fixture, FixtureBake.Block, [FixtureBake.Plate, [sphere, sphere with { Offset = -sphere.Offset }]]);
        written = written with { Mechanical = written.Mechanical! with { Hinges = [hinge] } };
        var json = BundleSchema.WriteEntries([written]);
        var read = Assert.Single(BundleSchema.Read(json).Elements).Mechanical!.MovingBodyColliders;
        AssertSame(written.Mechanical.MovingBodyColliders, read, "MovingBodyColliders");
        Assert.Equal([ColliderShapeKind.Box, ColliderShapeKind.Sphere], read.Select(body => body[0].ShapeKind));
        Assert.Equal([1, 2], read.Select(body => body.Count));
        var nullList = JsonNode.Parse(json)!;
        nullList["Elements"]![0]!["Mechanical"]!["MovingBodyColliders"]![1] = null;
        Assert.Throws<JsonException>(() => BundleSchema.Read(nullList.ToJsonString()));
        var missing = JsonNode.Parse(json)!;
        Assert.True(missing["Elements"]![0]!["Mechanical"]!.AsObject().Remove("MovingBodyColliders"));
        Assert.Throws<JsonException>(() => BundleSchema.Read(missing.ToJsonString()));
    }

    [Fact]
    public void ACompliantSurfaceRoundTripsBitForBit()
    {
        var fixture = new CompliantFixture();
        var json = Write(fixture);
        Assert.Contains("\"CompliantSurface\":", json, StringComparison.Ordinal);
        var entry = Assert.Single(BundleSchema.Read(json).Elements);
        AssertEntry(fixture, entry);
        Assert.NotNull(entry.Mechanical!.CompliantSurface);
    }

    [Fact]
    public void GenerationIsDeterministicAndCarriesNoTimestamp()
    {
        var first = BundleGenerator.Generate(new CuriousContraptions.Bundle.BundleServices(), TestRepository.Root);
        var second = BundleGenerator.Generate(new CuriousContraptions.Bundle.BundleServices(), TestRepository.Root);
        Assert.Empty(first.Errors);
        Assert.Equal(PackageDiscovery.PackageNames(TestRepository.Root).Count, first.Count);
        Assert.Equal(first.Json, second.Json);
        using var document = JsonDocument.Parse(first.Json!);
        Assert.Equal(["Elements"], document.RootElement.EnumerateObject().Select(property => property.Name));
    }

    [Fact]
    public void VectorsAndQuaternionsAreArraysAndEnumsAreNames()
    {
        var json = BundleSchema.WriteEntries([FixtureBake.Entry(new Fixture(), [Fixture.Box(new(1.5f, .25f, 3f))], [])]);
        using var document = JsonDocument.Parse(json);
        var element = document.RootElement.GetProperty("Elements")[0];
        var collider = element.GetProperty("Colliders")[0];
        Assert.Equal("[1.5,0.25,3]", collider.GetProperty("HalfExtents").GetRawText().Replace("\n", "", StringComparison.Ordinal).Replace(" ", "", StringComparison.Ordinal));
        Assert.Equal(4, collider.GetProperty("Rotation").GetArrayLength());
        Assert.Equal("Box", collider.GetProperty("ShapeKind").GetString());
        Assert.Equal("Structure", element.GetProperty("Metadata").GetProperty("Category").GetString());
        Assert.Equal("Mechanical", element.GetProperty("Domains")[0].GetString());
    }

    [Theory]
    [InlineData("\"Category\": \"Structure\"", "\"Category\": \"Sideways\"")]
    [InlineData("\"Category\": \"Structure\"", "\"Category\": 5")]
    [InlineData("\"Category\": \"Structure\"", "\"Category\": \"Motion, Goals\"")]
    [InlineData("\"Category\": \"Structure\"", "\"Category\": \"structure\"")]
    [InlineData("\"ShapeKind\": \"Box\"", "\"ShapeKind\": \"box\"")]
    [InlineData("\"KinematicMode\": \"Static\"", "\"KinematicMode\": \" Static\"")]
    [InlineData("\"KinematicMode\": \"Static\"", "\"KinematicMode\": \"Kinematic\"")]
    [InlineData("\"ScenePath\":", "\"Unknown\": 1, \"ScenePath\":")]
    [InlineData("\"HalfExtents\": [\n", "\"HalfExtents\": [\n 1,\n")]
    public void MalformedInputIsRejected(string original, string replacement)
    {
        var json = BundleSchema.WriteEntries([FixtureBake.Entry(new Fixture())]);
        Assert.Contains(original, json, StringComparison.Ordinal);
        Assert.Throws<JsonException>(() => BundleSchema.Read(json.Replace(original, replacement, StringComparison.Ordinal)));
    }

    // Story 9A.22: an event kind is written by its capability name with no payload layout; the deleted element-named kinds
    // (CounterReached, LatchSet), the deferred PrismaticLimit, a number and a retired PayloadLayout field are refused, with no alias.
    [Theory]
    [InlineData("\"Kind\": \"Impact\"", "\"Kind\": \"CounterReached\"")]
    [InlineData("\"Kind\": \"Impact\"", "\"Kind\": \"LatchSet\"")]
    [InlineData("\"Kind\": \"Impact\"", "\"Kind\": \"PrismaticLimit\"")]
    [InlineData("\"Kind\": \"Impact\"", "\"Kind\": 1")]
    [InlineData("\"Kind\": \"Impact\"", "\"EventKind\": \"Impact\"")]
    [InlineData("\"Kind\": \"Impact\"", "\"Kind\": \"Impact\", \"PayloadLayout\": \"Impulse:f32\"")]
    public void AnEventKindIsItsCapabilityNameAndRetiredNamesAreRefused(string original, string replacement)
    {
        var json = Write(new Fixture { Events = [new(SimulationEventKind.Impact)] });
        Assert.Contains(original, json, StringComparison.Ordinal);
        Assert.Equal([new EventDescriptor(SimulationEventKind.Impact)], Assert.Single(BundleSchema.Read(json).Elements).Events);
        Assert.Throws<JsonException>(() => BundleSchema.Read(json.Replace(original, replacement, StringComparison.Ordinal)));
    }


    [Fact]
    public void ImpulsePeakRoundTripsExplicitlyAndRefusesMalformedOrInactiveLanes()
    {
        var json = Write(Declaration("Cannon"));
        var entry = Assert.Single(BundleSchema.Read(json).Elements);
        var pulse = Assert.Single(entry.Presentation.Bindings, binding => binding.Curve == PresentationCurve.SmoothRiseFall);
        Assert.Equal(.08f / .53f, pulse.ImpulsePeak!.Value.Value);
        entry = SceneGeometryExtractor.Bake(File.ReadAllText(Path.Combine(TestRepository.Root, "elements/Cannon/cannon.tscn")),
            entry, Declaration("Cannon").SceneBodies);
        Assert.Empty(BundleValidator.Validate([new("Cannon", entry)]));
        using var document = JsonDocument.Parse(json);
        var encoded = document.RootElement.GetProperty("Elements")[0].GetProperty("Presentation").GetProperty("Bindings")
            .EnumerateArray().Single(binding => binding.GetProperty("Curve").GetString() == "SmoothRiseFall")
            .GetProperty("ImpulsePeak").GetRawText();
        foreach (var malformed in new[] { "{}", "{\"Value\":0.2,\"Value\":0.3}", "{\"Other\":0.2}",
            "{\"Value\":-0.1}", "{\"Value\":1.1}", "{\"Value\":\"0.2\"}", "{\"Value\":0.2,\"Other\":0}" })
            Assert.Throws<JsonException>(() => BundleSchema.Read(json.Replace(encoded, malformed, StringComparison.Ordinal)));
        foreach (var invalid in new[] { pulse with { ImpulsePeak = null }, pulse with { Curve = PresentationCurve.SineSquaredPulse,
            Overlap = PresentationOverlap.SaturatingSum }, pulse with { ImpulsePeak = new(0f) } })
        {
            var changed = entry with { Presentation = entry.Presentation with { Bindings = [invalid] } };
            Assert.NotEmpty(BundleValidator.Validate([new("Cannon", changed)]));
        }
    }

    /// <summary>The entry's declared fields equal the declaration's. Colliders are not declared: the scene bake writes them, and the
    /// tests above compare them with the entry that was written.</summary>
    private static void AssertEntry(IPuzzleElementDeclaration declaration, BundleEntry entry)
    {
        AssertSame(declaration.Metadata, entry.Metadata, "Metadata");
        Assert.Equal(declaration.SupportedDomains, entry.Domains.Aggregate(SimulationDomain.None, (all, domain) => all | domain));
        AssertSame(declaration.Footprint, entry.Footprint, "Footprint");
        AssertSame(declaration.Ports, entry.Ports, "Ports");
        AssertSame(declaration.Events, entry.Events, "Events");
        AssertSame(declaration.Presentation, entry.Presentation, "Presentation");
        Assert.Equal(declaration is IMechanicalElement, entry.Mechanical is not null);
        if (declaration is IMechanicalElement mechanical)
        {
            Assert.Equal(mechanical.KinematicMode, entry.Mechanical!.KinematicMode);
            AssertSame(mechanical.MassFrame, entry.Mechanical.MassFrame, "MassFrame");
            AssertSame(mechanical.AnchorPoints, entry.Mechanical.AnchorPoints, "AnchorPoints");
            AssertSame(mechanical is ISliderElement slider ? slider.Sliders : [], entry.Mechanical.Sliders, "Sliders");
            AssertSame(mechanical is IHingeElement hinge ? hinge.Hinges : [], entry.Mechanical.Hinges, "Hinges");
            AssertSame(mechanical is IMechanicalTransmissionElement transmissions ? transmissions.Transmissions : [], entry.Mechanical.Transmissions, "Transmissions");
            AssertSame(mechanical is IPrismaticRetentionElement retentions ? retentions.Retentions : [], entry.Mechanical.Retentions, "Retentions");
            AssertSame(mechanical is IContactWorkElement work ? work.ContactWorks : [], entry.Mechanical.ContactWorks, "ContactWorks");
            AssertSame(mechanical is IEnergyStoreElement stores ? stores.EnergyStores : [], entry.Mechanical.EnergyStores, "EnergyStores");
            AssertSame(mechanical is IChamberRegionElement chamber ? chamber.ChamberRegions : [], entry.Mechanical.ChamberRegions, "ChamberRegions");
            AssertSame(mechanical is IFiniteWorkActuatorElement actuator ? actuator.FiniteWorkActuators : [], entry.Mechanical.FiniteWorkActuators, "FiniteWorkActuators");
            Assert.Equal(mechanical is ICompliantSurfaceElement, entry.Mechanical.CompliantSurface is not null);
            if (mechanical is ICompliantSurfaceElement compliant) AssertSame(compliant.CompliantSurface, entry.Mechanical.CompliantSurface!.Value, "CompliantSurface");
            Assert.Equal(mechanical.Payload, entry.Mechanical.Payload);
        }
        Assert.Equal(declaration is IElectricalElement, entry.Electrical is not null);
        if (declaration is IElectricalElement electrical) AssertSame(electrical.ElectricalNodes, entry.Electrical!.Nodes, "ElectricalNodes");
        Assert.Equal(declaration is Abstractions.Electrical.ISupplyElement, entry.Electrical?.SupplyGraph is not null);
        // The supply capability, a Source's finite store included (Story 9A.39).
        if (declaration is Abstractions.Electrical.ISupplyElement supply) AssertSame(supply.SupplyGraph, entry.Electrical!.SupplyGraph!, "SupplyGraph");
        Assert.Equal(declaration is ISignalElement, entry.Signal is not null);
        if (declaration is ISignalElement signal)
        {
            AssertSame(signal.SignalNodes, entry.Signal!.Nodes, "Nodes");
            AssertSame(signal.SignalEdges, entry.Signal.Edges, "Edges");
            AssertSame(signal.SignalPorts, entry.Signal.Ports, "Ports");
        }
        Assert.Equal(declaration is IFluidElement, entry.Fluid is not null);
        if (declaration is IFluidElement fluid)
        {
            AssertSame(fluid.FluidProperties, entry.Fluid!.Properties, "FluidProperties");
            AssertSame(fluid.FluidPorts, entry.Fluid.Ports, "FluidPorts");
        }
    }

    /// <summary>Deep equality with f32 compared by bits: vectors and quaternions by component, lists by element, records by every
    /// public property.</summary>
    private static void AssertSame(object? expected, object? actual, string path)
    {
        if (expected is null || actual is null)
        {
            Assert.True(expected is null && actual is null, $"{path}: {expected} vs {actual}");
            return;
        }
        switch (expected)
        {
            case float value:
                Assert.True(BitConverter.SingleToInt32Bits(value) == BitConverter.SingleToInt32Bits((float)actual),
                    $"{path}: {value:R} vs {(float)actual:R}");
                return;
            case Vector2 vector2:
                var other2 = (Vector2)actual;
                AssertSame(vector2.X, other2.X, path + ".x"); AssertSame(vector2.Y, other2.Y, path + ".y");
                return;
            case Vector3 vector:
                var other = (Vector3)actual;
                AssertSame(vector.X, other.X, path + ".x"); AssertSame(vector.Y, other.Y, path + ".y"); AssertSame(vector.Z, other.Z, path + ".z");
                return;
            case Quaternion rotation:
                var turned = (Quaternion)actual;
                AssertSame(rotation.X, turned.X, path + ".x"); AssertSame(rotation.Y, turned.Y, path + ".y");
                AssertSame(rotation.Z, turned.Z, path + ".z"); AssertSame(rotation.W, turned.W, path + ".w");
                return;
            case string or Enum or int or uint or bool:
                Assert.Equal(expected, actual);
                return;
            case IEnumerable list:
                var left = list.Cast<object?>().ToArray();
                var right = ((IEnumerable)actual).Cast<object?>().ToArray();
                Assert.True(left.Length == right.Length, $"{path}: {left.Length} vs {right.Length} items");
                for (var i = 0; i < left.Length; i++) AssertSame(left[i], right[i], $"{path}[{i}]");
                return;
        }
        Assert.Equal(expected.GetType(), actual.GetType());
        var properties = expected.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetIndexParameters().Length == 0).ToArray();
        Assert.NotEmpty(properties);
        foreach (var property in properties) AssertSame(property.GetValue(expected), property.GetValue(actual), $"{path}.{property.Name}");
    }
}
