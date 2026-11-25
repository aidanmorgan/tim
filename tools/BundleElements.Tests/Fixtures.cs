using System.Numerics;
using CuriousContraptions.Abstractions;
using CuriousContraptions.Abstractions.Mechanical;
using CuriousContraptions.Abstractions.Signal;
using CuriousContraptions.Bundle;

namespace CuriousContraptions.BundleElements.Tests;

/// <summary>A configurable mechanical fixture declaration, compiled only into this test project. Its defaults are valid; its baked
/// entry is <see cref="FixtureBake.Entry(Fixture)"/>.</summary>
public sealed class Fixture : ISliderElement
{
    /// <summary>A valid slider: a 0.25 kg plate that may travel 0.25 m below rest, on the fixture's three spring parameters.</summary>
    public static PrismaticSliderDescriptor Slider(string stiffness = "stiffness") =>
        new(new(new(0f, .14f, 0f), Quaternion.Identity), new(Vector3.Zero, Quaternion.Identity), MechanicalValue.Fixed(-.25f), MechanicalValue.Fixed(0f), MechanicalValue.FromParameter(stiffness), MechanicalValue.FromParameter("damping"),
            MechanicalValue.FromParameter("initial_compression"), false, new(MassFrameDeclaration.Constant(.25f, Vector3.Zero, new(.03f, .06f, .04f), Quaternion.Identity, 0f, new(0f, -9.81f, 0f))), null);

    /// <summary>A valid supplied drive for <see cref="Slider"/>: target 0.1 m below rest, inside its [-0.25, 0] travel.</summary>
    public static PrismaticDriveDescriptor Drive() =>
        new(MechanicalValue.Fixed(-.1f), MechanicalValue.Fixed(1.5f), MechanicalValue.Fixed(6f), MechanicalValue.Fixed(20f), MechanicalValue.Fixed(15f));

    /// <summary>An electrical PowerIn input port, which a driven slider requires.</summary>
    public static PortDescriptor PowerIn =>
        new(PortId.PowerIn, SimulationDomain.Electrical, PortDirection.Input, PortRole.PowerIn, new("PowerIn", SocketGlyph.Text), null, Vector3.Zero, Vector3.UnitZ);

    public static IReadOnlyList<ParameterDescriptor> SpringParameters =>
    [
        new("stiffness", ParameterUnit.NewtonPerMetre, ParameterKind.Real, 120f, 1200f, 400f, []),
        new("damping", ParameterUnit.NewtonSecondPerMetre, ParameterKind.Real, 0f, 8f, .2f, []),
        new("initial_compression", ParameterUnit.Metre, ParameterKind.Real, 0f, .2f, 0f, [])
    ];

    public static readonly MaterialProperties Material = new(1f, .3f, 0f, .1f);

    public static ColliderDescriptor Box(Vector3 halfExtents) =>
        new(ColliderShapeKind.Box, halfExtents, Vector3.Zero, Quaternion.Identity, Material, 0f);

    public string Id { get; init; } = "fixture";
    public ElementCategory Category { get; init; } = ElementCategory.Structure;
    public IReadOnlyList<ParameterDescriptor> Parameters { get; init; } = [];
    public string DisplayName { get; init; } = "Fixture";
    public string Description { get; init; } = "A fixture.";
    public ElementColor Color { get; init; } = new(.5f, .5f, .5f, 1f);
    public int PaletteOrder { get; init; } = 1;
    public string IconPath { get; init; } = "res://elements/Fixture/icon.svg";
    public string ScenePath { get; init; } = "res://elements/Fixture/fixture.tscn";
    public ElementPresentation Presentation => new(1f, PresentationAxes.None, [],
        Sliders.Select((_, index) => new PresentationBodyBinding($"Visual/Body{index + 1}", new(index + 1), Vector3.Zero)).ToArray(), [], []);
    public ElementMetadata Metadata => new(Id, Category, DisplayName, Description, Color, PaletteOrder, IconPath, ScenePath, Parameters);
    public ElementFootprint Footprint { get; init; } = new(1, 0, 0, 0);
    public IReadOnlyList<PortDescriptor> Ports { get; init; } = [];
    public IReadOnlyList<EventDescriptor> Events { get; init; } = [];
    public SimulationDomain SupportedDomains { get; init; } = SimulationDomain.Mechanical;
    /// <summary>The base at the scene root, then each slider's body under the node its presentation moves.</summary>
    public IReadOnlyList<SceneBodyGeometry> SceneBodies =>
        [new(".", Material), .. Sliders.Select((_, index) => new SceneBodyGeometry($"Visual/Body{index + 1}", Material))];
    public KinematicMode KinematicMode { get; init; } = KinematicMode.Static;
    public MassFrameDeclaration MassFrame { get; init; } = MassFrameDeclaration.Constant(0f, Vector3.Zero, Vector3.Zero, Quaternion.Identity, 0f, Vector3.Zero);
    public PayloadPolicy Payload { get; init; } = PayloadPolicy.None;
    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints { get; init; } = [];
    public IReadOnlyList<PrismaticSliderDescriptor> Sliders { get; init; } = [];
}

/// <summary>Stands in for the scene bake on fixture declarations, which have no scenes: a fixture's entry carries the colliders its
/// scene would bake, on the base body and, in package body order (sliders, then hinges), on each moving body.</summary>
internal static class FixtureBake
{
    /// <summary>The block on <see cref="Fixture"/>'s base body.</summary>
    public static readonly IReadOnlyList<ColliderDescriptor> Block = [Fixture.Box(new(.5f, .5f, .5f))];
    /// <summary>The plate on each <see cref="Fixture.Slider"/>'s moving body.</summary>
    public static readonly IReadOnlyList<ColliderDescriptor> Plate = [Fixture.Box(new(.65f, .075f, .6f))];
    /// <summary>The lamp housing on <see cref="SignalFixture"/>'s body.</summary>
    public static readonly IReadOnlyList<ColliderDescriptor> Lamp = [Fixture.Box(new(.4f, .1f, .4f))];
    /// <summary>The bed on <see cref="CompliantFixture"/>'s body.</summary>
    public static readonly IReadOnlyList<ColliderDescriptor> Bed = [Fixture.Box(new(1.2f, .1f, .9f))];

    /// <summary>The declaration's entry with <paramref name="colliders"/> on its base body and, when it is mechanical,
    /// <paramref name="moving"/> as its moving-body collider lists.</summary>
    public static BundleEntry Entry(IPuzzleElementDeclaration declaration, IReadOnlyList<ColliderDescriptor> colliders,
        IReadOnlyList<IReadOnlyList<ColliderDescriptor>> moving)
    {
        var entry = BundleSchema.Entry(declaration) with { Colliders = colliders };
        return entry.Mechanical is { } mechanical ? entry with { Mechanical = mechanical with { MovingBodyColliders = moving } } : entry;
    }

    /// <summary>A <see cref="Fixture"/> as its scene bakes: the block on its base body and the plate on each slider's body.</summary>
    public static BundleEntry Entry(Fixture fixture) => Entry(fixture, Block, [.. fixture.Sliders.Select(_ => Plate)]);
}

/// <summary>A second public declaration in this assembly, so the assembly as a package declares two (BUNDLE005).</summary>
public sealed class SecondFixture : IPuzzleElementDeclaration
{
    public ElementPresentation Presentation => new(1f, PresentationAxes.None, [], [], [], []);
    public ElementMetadata Metadata => new("second", ElementCategory.Goals, "Second", "A second fixture.", new(.5f, .5f, .5f, 1f), 2, "res://elements/Second/icon.svg", "res://elements/Second/second.tscn", []);
    public ElementFootprint Footprint => new(0, 0, 0, 0);
    public IReadOnlyList<PortDescriptor> Ports => [];
    public IReadOnlyList<EventDescriptor> Events => [];
    public SimulationDomain SupportedDomains => SimulationDomain.Signal;
    public IReadOnlyList<SceneBodyGeometry> SceneBodies => [new(".", Fixture.Material)];
}

/// <summary>A configurable non-mechanical (signal) fixture, shaped like the Signal lamp: it has a body and colliders
/// (<see cref="FixtureBake.Lamp"/>) but no mechanical payload. Internal, so the assembly-as-package discovery count above stays
/// two.</summary>
internal sealed class SignalFixture : ISignalElement
{
    public ElementPresentation Presentation => new(1f, PresentationAxes.None, [], [], [], []);
    public ElementMetadata Metadata => new("signal_fixture", ElementCategory.Goals, "Signal fixture", "A signal fixture.", new(.5f, .5f, .5f, 1f), 3, "res://elements/SignalFixture/icon.svg",
        "res://elements/SignalFixture/signal.tscn", []);
    public ElementFootprint Footprint { get; init; } = new(1, 0, 0, 0);
    public IReadOnlyList<PortDescriptor> Ports { get; init; } = [];
    public IReadOnlyList<EventDescriptor> Events => [];
    public SimulationDomain SupportedDomains => SimulationDomain.Signal;
    public IReadOnlyList<SceneBodyGeometry> SceneBodies => [new(".", Fixture.Material)];
    public IReadOnlyList<SignalNodeDescriptor> SignalNodes { get; init; } = [];
    public IReadOnlyList<SignalEdge> SignalEdges => [];
    public IReadOnlyList<SignalPortBinding> SignalPorts { get; init; } = [];
}

/// <summary>A compliant-surface fixture whose body carries <see cref="FixtureBake.Bed"/>.</summary>
internal sealed class CompliantFixture : ICompliantSurfaceElement
{
    public ElementPresentation Presentation => new(1f, PresentationAxes.None, [], [], [], []);
    public ElementMetadata Metadata => new("compliant_fixture", ElementCategory.Motion, "Compliant fixture", "A compliant fixture.",
        new(.4f, .72f, .79f, 1f), 35, "res://elements/CompliantFixture/icon.svg", "res://elements/CompliantFixture/compliant.tscn",
        [
            new("tension", ParameterUnit.NewtonPerMetre, ParameterKind.Real, 120f, 1200f, 180f, []),
            new("damping_ratio", ParameterUnit.None, ParameterKind.Real, 0.08f, 0.8f, 0.12f, [])
        ]);
    public ElementFootprint Footprint { get; init; } = new(1, 0, 0, 0);
    public IReadOnlyList<PortDescriptor> Ports => [];
    public IReadOnlyList<EventDescriptor> Events => [];
    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical;
    public IReadOnlyList<SceneBodyGeometry> SceneBodies => [new(".", Fixture.Material)];
    public KinematicMode KinematicMode => KinematicMode.Static;
    public MassFrameDeclaration MassFrame => MassFrameDeclaration.Constant(0f, Vector3.Zero, Vector3.Zero, Quaternion.Identity, 0f, Vector3.Zero);
    public PayloadPolicy Payload => PayloadPolicy.None;
    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];
    public CompliantSurfaceDescriptor CompliantSurface { get; init; } = new(
        new(1.2f, 0.9f), 0.2f, 0.65f, 0.1f, MechanicalValue.FromParameter("tension"), MechanicalValue.FromParameter("damping_ratio"));
}

/// <summary>A fixture whose scene declares body roots (Story 10.3a): the base at the scene root and one moving body under
/// <c>Visual/Beam</c>, so a port may name body 1 and keep its locator under that body. Internal, so the assembly-as-package discovery
/// count above stays two.</summary>
internal sealed class SceneBodyFixture : IPuzzleElementDeclaration
{
    public ElementPresentation Presentation => new(1f, PresentationAxes.None, [], [], [], []);
    public ElementMetadata Metadata => new("fixture", ElementCategory.Motion, "Fixture", "A fixture.", new(.5f, .5f, .5f, 1f), 1,
        "res://elements/Fixture/icon.svg", "res://elements/Fixture/fixture.tscn", []);
    public ElementFootprint Footprint => new(2, 1, 0, 1);
    public IReadOnlyList<PortDescriptor> Ports { get; init; } = [];
    public IReadOnlyList<EventDescriptor> Events => [];
    public SimulationDomain SupportedDomains => SimulationDomain.Rope;
    public IReadOnlyList<SceneBodyGeometry> SceneBodies => [new(".", new(1f, .3f, 0f, .1f)), new("Visual/Beam", new(0f, .3f, 0f, .1f))];
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

/// <summary>A static fixture whose one body takes its colliders from its scene (the base at the scene root), so a scene's proxies reach
/// the extractor through the generator. Its identity and paths are <see cref="Fixture"/>'s. Internal, so the assembly-as-package
/// discovery count above stays two.</summary>
internal sealed class SceneOwnedFixture : IMechanicalElement
{
    public ElementPresentation Presentation => new(1f, PresentationAxes.None, [], [], [], []);
    public ElementMetadata Metadata => new Fixture().Metadata;
    public ElementFootprint Footprint => new(1, 0, 0, 0);
    public IReadOnlyList<PortDescriptor> Ports { get; init; } = [];
    public IReadOnlyList<EventDescriptor> Events => [];
    public SimulationDomain SupportedDomains => SimulationDomain.Mechanical;
    public KinematicMode KinematicMode => KinematicMode.Static;
    public MassFrameDeclaration MassFrame => MassFrameDeclaration.Constant(0f, Vector3.Zero, Vector3.Zero, Quaternion.Identity, 0f, Vector3.Zero);
    public PayloadPolicy Payload => PayloadPolicy.None;
    public IReadOnlyList<AnchorPointDescriptor> AnchorPoints => [];
    public IReadOnlyList<SceneBodyGeometry> SceneBodies => [new(".", new(1f, .3f, 0f, .1f))];
}
