using System.Collections;
using System.Numerics;
using System.Reflection;
using System.Text.Json;
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

    [Theory]
    [MemberData(nameof(Packages))]
    public void EveryPackageEqualsItsDeserialisedEntry(string package)
    {
        var declaration = Declaration(package);
        var entry = Assert.Single(BundleSchema.Read(BundleSchema.Write([declaration])).Elements);
        AssertEntry(declaration, entry);
    }

    [Fact]
    public void SignedZeroAndSubnormalValuesRoundTripBitForBit()
    {
        var subnormal = BitConverter.Int32BitsToSingle(1);
        var fixture = new Fixture
        {
            Colliders = [new(ColliderShapeKind.Box, new(-0f, subnormal, .1f), new(float.MaxValue, -subnormal, float.Epsilon),
                new(-0f, 0f, 0f, 1f), new(-0f, subnormal, 0f, 1e-7f))],
            MassProperties = new(-0f, new(subnormal, -0f, 3.4028235e38f), new(1f / 3f, 0.1f, 16777217f), -subnormal, new(0, -9.81f, 0))
        };
        var json = BundleSchema.Write([fixture]);
        Assert.Contains("-0", json, StringComparison.Ordinal);
        Assert.Contains("1E-45", json, StringComparison.Ordinal);
        AssertEntry(fixture, Assert.Single(BundleSchema.Read(json).Elements));
    }

    [Fact]
    public void GenerationIsDeterministicAndCarriesNoTimestamp()
    {
        var first = BundleGenerator.Generate(TestRepository.Root);
        var second = BundleGenerator.Generate(TestRepository.Root);
        Assert.Empty(first.Errors);
        Assert.Equal(PackageDiscovery.PackageNames(TestRepository.Root).Count, first.Count);
        Assert.Equal(first.Json, second.Json);
        using var document = JsonDocument.Parse(first.Json!);
        Assert.Equal(["Elements"], document.RootElement.EnumerateObject().Select(property => property.Name));
    }

    [Fact]
    public void VectorsAndQuaternionsAreArraysAndEnumsAreNames()
    {
        var json = BundleSchema.Write([new Fixture { Colliders = [Fixture.Box(new(1.5f, .25f, 3f))] }]);
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
    [InlineData("\"ScenePath\":", "\"Unknown\": 1, \"ScenePath\":")]
    [InlineData("\"HalfExtents\": [\n", "\"HalfExtents\": [\n 1,\n")]
    public void MalformedInputIsRejected(string original, string replacement)
    {
        var json = BundleSchema.Write([new Fixture()]);
        Assert.Contains(original, json, StringComparison.Ordinal);
        Assert.Throws<JsonException>(() => BundleSchema.Read(json.Replace(original, replacement, StringComparison.Ordinal)));
    }

    private static void AssertEntry(IPuzzleElementDeclaration declaration, BundleEntry entry)
    {
        AssertSame(declaration.Metadata, entry.Metadata, "Metadata");
        Assert.Equal(declaration.SupportedDomains, entry.Domains.Aggregate(SimulationDomain.None, (all, domain) => all | domain));
        AssertSame(declaration.Footprint, entry.Footprint, "Footprint");
        AssertSame(declaration.Ports, entry.Ports, "Ports");
        AssertSame(declaration.Colliders, entry.Colliders, "Colliders");
        AssertSame(declaration.Events, entry.Events, "Events");
        Assert.Equal(declaration is IMechanicalElement, entry.Mechanical is not null);
        if (declaration is IMechanicalElement mechanical)
        {
            Assert.Equal(mechanical.KinematicMode, entry.Mechanical!.KinematicMode);
            AssertSame(mechanical.MassProperties, entry.Mechanical.MassProperties, "MassProperties");
            AssertSame(mechanical.AnchorPoints, entry.Mechanical.AnchorPoints, "AnchorPoints");
        }
        Assert.Equal(declaration is IElectricalElement, entry.Electrical is not null);
        if (declaration is IElectricalElement electrical) AssertSame(electrical.ElectricalNodes, entry.Electrical!.Nodes, "ElectricalNodes");
        Assert.Equal(declaration is ISignalElement, entry.Signal is not null);
        if (declaration is ISignalElement signal)
        {
            AssertSame(signal.Triggers, entry.Signal!.Triggers, "Triggers");
            Assert.Equal(signal.LatchMode, entry.Signal.LatchMode);
            AssertSame(signal.Counter, entry.Signal.Counter, "Counter");
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
            case Vector3 vector:
                var other = (Vector3)actual;
                AssertSame(vector.X, other.X, path + ".x"); AssertSame(vector.Y, other.Y, path + ".y"); AssertSame(vector.Z, other.Z, path + ".z");
                return;
            case Quaternion rotation:
                var turned = (Quaternion)actual;
                AssertSame(rotation.X, turned.X, path + ".x"); AssertSame(rotation.Y, turned.Y, path + ".y");
                AssertSame(rotation.Z, turned.Z, path + ".z"); AssertSame(rotation.W, turned.W, path + ".w");
                return;
            case string or Enum or int or bool:
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
