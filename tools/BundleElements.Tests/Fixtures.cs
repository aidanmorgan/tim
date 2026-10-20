using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;

namespace CuriousContraptions.BundleElements.Tests;

/// <summary>A configurable mechanical fixture declaration, compiled only into this test project. Its defaults are valid.</summary>
public sealed class Fixture : IMechanicalElement
{
    public static ColliderDescriptor Box(Vector3 halfExtents) =>
        new(ColliderShapeKind.Box, halfExtents, Vector3.Zero, Quaternion.Identity, new MaterialProperties(1f, .3f, 0f, .1f));

    public string Id { get; init; } = "fixture";
    public ElementCategory Category { get; init; } = ElementCategory.Structure;
    public IReadOnlyList<ParameterDescriptor> Parameters { get; init; } = [];
    public ElementMetadata Metadata => new(Id, Category, "part_fixture", null, "res://elements/Fixture/fixture.tscn", Parameters);
    public ElementFootprint Footprint { get; init; } = new(1, 0, 0);
    public IReadOnlyList<PortDescriptor> Ports { get; init; } = [];
    public IReadOnlyList<ColliderDescriptor> Colliders { get; init; } = [Box(new(.5f, .5f, .5f))];
    public IReadOnlyList<EventDescriptor> Events { get; init; } = [];
    public SimulationDomain SupportedDomains { get; init; } = SimulationDomain.Mechanical;
    public KinematicMode KinematicMode { get; init; } = KinematicMode.Static;
    public RigidBodyMassProperties MassProperties { get; init; } = new(0f, Vector3.Zero, Vector3.Zero, 0f, Vector3.Zero);
    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints { get; init; } = [];
}

/// <summary>A second public declaration in this assembly, so the assembly as a package declares two (BUNDLE005).</summary>
public sealed class SecondFixture : IPuzzleElementDeclaration
{
    public ElementMetadata Metadata => new("second", ElementCategory.Goals, "part_second", null, "res://second.tscn", []);
    public ElementFootprint Footprint => new(0, 0, 0);
    public IReadOnlyList<PortDescriptor> Ports => [];
    public IReadOnlyList<ColliderDescriptor> Colliders => [];
    public IReadOnlyList<EventDescriptor> Events => [];
    public SimulationDomain SupportedDomains => SimulationDomain.Signal;
}

internal static class TestRepository
{
    public static readonly string Root = FindRoot();

    private static string FindRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
            if (File.Exists(Path.Combine(directory.FullName, "CuriousContraptions.slnx"))) return directory.FullName;
        throw new InvalidOperationException("The repository root (CuriousContraptions.slnx) is not above the test output.");
    }

    public static string Read(string relative) => File.ReadAllText(Path.Combine(Root, relative));
}
